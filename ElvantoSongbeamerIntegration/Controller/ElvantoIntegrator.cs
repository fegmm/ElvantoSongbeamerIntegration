using ElvantoApi.Models;
using ElvantoSongbeamerIntegration.Controller;
using ElvantoSongbeamerIntegration.Model;
using Newtonsoft.Json.Linq;
using SongbeamerSongbookIntegrator.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SongbeamerSongbookIntegrator.Controller
{
    // Beware https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md
    public class ElvantoIntegrator
    {
        #region Variablen
        private static ServicesGetAllResponse AllServicesResponse;
        private static ServiceGetDetailsResponse ServiceDetailsResponse;
        private static DetectLanguage LanguageDetector;
        private ElvantoApi.Client ElvantoClient;

        public string Errors { get; private set; } = "";
        public string Warnings { get; private set; } = "";
        public bool IsInitialized { get; private set; } = false;

        public delegate void IntegrationFinished(object sender, EventArgs e);
        public event IntegrationFinished IntegrationFinishedEvent;
        #endregion


        public ElvantoIntegrator()
        {
        }

        private async Task<bool> InitLanguageDetection()
        {
            LanguageDetector = new DetectLanguage();
            var result = await LanguageDetector.Init();

            return result;
        }

        public async Task<bool> Init()
        {
            var key = GetApiKey();
            ElvantoClient = new ElvantoApi.Client(key);
            AllServicesResponse = await ElvantoClient.ServicesGetAllNoFieldsAsync();

            await InitLanguageDetection();

            IsInitialized = true;

            return true;
        }

        private string GetApiKey()
        {
            var stream = new StreamReader(@"C:\Users\Johannes\Documents\Visual Studio 2017\Projects\ElvantoSongbeamerIntegration\ElvantoSongbeamerIntegration\ApiKey.txt");
            var key = stream.ReadLine().ToCharArray();
            stream.Close();
            stream.Dispose();

            // Verschlüsselten Schlüssel entschlüsseln
            for (var i = 0; i < key.Length - 1; i++)
            {
                key[i] -= (char)i;
            }

            return new string(key);
        }

        public async Task<bool> CreateScheduleForService(DateTime dateTime, ServiceTemplateType serviceType, bool openSongbeamer)
        {
            if (!IsInitialized || AllServicesResponse.services == null) { Errors = "Elvanto-Integrator ist noch nicht fertig initialisiert."; return false; }

            // Datum in für Elvanto passendes Format (inkl. Zeitzone UTC0) konvertieren
            dateTime = dateTime.AddHours(-1);
            var dateIdString = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            // Passenden Gottesdienst suchen und Details in Elvanto abrufen (erst hier, um Zeit zu sparen)
            var singleService = AllServicesResponse.services.service.Where(x => x.date == dateIdString);
            if (singleService.Count() == 0) { Errors = "Gottesdienst konnte nicht gefunden werden!"; return false; }
            if (singleService.Count() > 1) { Errors = "Gottesdienst-Angaben waren nicht eindeutig: " + ServiceItem.NewLine + string.Join(",", singleService.Select(x => x.date)); return false; }

            ServiceDetailsResponse = await ElvantoClient.ServiceGetDetailsAsync(singleService.First().id);
            if (!ServiceDetailsResponse.status.Equals("ok")) { Errors = "Gottesdienst-Details konnten nicht richtig gefunden werden!"; return false; }
            var service = ServiceDetailsResponse.service.First();


            // Input für Ablaufplan-Creator erstellen
            var isBistro = service.volunteers.plan[0].positions.position.Where(x => x.position_name == "M12 Bistro Chef").FirstOrDefault()?.volunteers != null;
            var planItems = service.plans.plan.FirstOrDefault().items.item.Where(x => x.heading == 0);

            var songsInput = "";
            foreach (var item in planItems)
            {
                // Ansagen oder Predigt
                var isAnnouncement = item.title.Contains("Ansagen");
                var isSermon = item.title.Contains("Predigt") && int.Parse(item.duration.Substring(0, 2)) >= 10;           // Es gibt auch "Hinführung zur Predigt", mind. 10 Minuten lang
                var isPrayer = item.title.Contains("Gebetsgemeinschaft");       // TODO auch "Gebet" zulassen?
                var isReading = item.title.Contains("Textlesung");
                var isLordsSupper = item.title.Contains("Abendmahl");
                var isVortragslied = item.title.Contains("Vortragslied") || item.title.Contains("Vortragsstück");

                if (isAnnouncement) { songsInput += "Ansagen" + ServiceItem.NewLine; }
                else if (isSermon) { songsInput += "Predigt" + ServiceItem.NewLine; }
                else if (isPrayer) { songsInput += "Gebetsanliegen" + ServiceItem.NewLine; }
                else if (isReading) { songsInput += "Textlesung" + ServiceItem.NewLine; }
                else if (isLordsSupper) { songsInput += "Abendmahl" + ServiceItem.NewLine; }
                else if (isVortragslied && Settings.Instance.ADD_NOTE_FOR_VORTRAGSLIED) { songsInput += "Vortragslied" + ServiceItem.NewLine; }
                else
                {
                    // In Elvanto existierendes Lied
                    if (item.song != null && item.song != "") // Vergleich muss so sein (nicht in String umwandeln!)
                    {
                        songsInput += GetCCLIOrTitleOfSong((JObject)item.song);
                    }
                    else if (item.title.Contains("Lied"))
                    {
                        // Lied-Information könnte entweder in der Beschreibung (Standard) oder vielleicht auch im Titel sein
                        var note = item.description.Replace("<p>", "").Replace("</p>", "").Replace("\"", "");

                        if (item.title.Substring("Lied".Length).Length > 3) 
                        {
                            // Titel könnte in Heading stehen
                        }
                        else if (!string.IsNullOrEmpty(note))
                        {
                            // Titel dürfte in Notizen stehen
                            var hasBrackets = note.Contains("(");

                            // Falls in Klammer keine Angabe zum Liederbuch ist, dann existiert Lied vermutlich in Songbeamer

                        }
                        else
                        {
                            // Keine Art des Auslesens bekannt oder Daten fehlen
                            Warnings += $"Element '{item.title}' ('{note}') konnte nicht verarbeitet werden.";
                        }
                    }
                }
            }

            // Gottesdienst-Ablaufplan für Songbeamer erstellen und danach Songbeamer öffnen
            var options = new ServiceCreatorOptions()
            {
                IsForYouth = false,
                OpenSongbeamer = openSongbeamer,
                PptAsSecondChance = false,
                RecognizeOptionalSongs = false,
                RecognizeSermon = false,
                UseCCLI = true
            };

            var serviceCreator = new ServiceCreator("", false);
            serviceCreator.Init(options, songsInput);

            var result = serviceCreator.IntegrateServiceSchudule(serviceType, isBistro);

            IntegrationFinishedEvent?.Invoke(this, new EventArgs());

            return result;
        }



        private string GetCCLIOrTitleOfSong(JObject song)
        {
            var arrangement = ((JObject)song.GetValue("arrangement")).GetValue("title").ToString();

            if (arrangement != "Standard Arrangement")
            {
                var title = song.GetValue("title").ToString();
                if (arrangement == "Englisch" || arrangement == "Deutsch")
                {
                    var titleSegments = title.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (titleSegments.Count() != 2) { return ""; }  // TODO:  Lied zu Fehlern hinzufügen
                    var lang1 = LanguageDetector.Detect(titleSegments[0]);
                    var lang2 = LanguageDetector.Detect(titleSegments[1]);

                    if ((lang1 == "Deutsch" && arrangement == "Deutsch") || (lang1 == "Englisch" && arrangement == "Englisch")) { title = titleSegments[0]; }
                    else if ((lang2 == "Deutsch" && arrangement == "Deutsch") || (lang2 == "Englisch" && arrangement == "Englisch")) { title = titleSegments[1]; }
                    else { return ""; } // TODO:  Lied zu Fehlern hinzufügen

                    return title + ServiceItem.NewLine;
                }
                else
                {
                    // TODO:  Auch Versreihenfolge mitgeben
                    var ccli = song.GetValue("ccli_number").ToString();
                    return ccli + ServiceItem.NewLine;
                }
            }
            else
            {
                // 
                var ccli = song.GetValue("ccli_number").ToString();
                return ccli + ServiceItem.NewLine;
            }
        }

    }
}
