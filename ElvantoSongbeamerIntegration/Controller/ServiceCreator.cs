using ElvantoSongbeamerIntegration.Model;
using Microsoft.Win32;
using SongbeamerSongbookIntegrator;
using SongbeamerSongbookIntegrator.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
namespace ElvantoSongbeamerIntegration.Controller
{
    public class ServiceCreator
    {
        #region Variables
        private bool              IsInitialized      = false;
        private ServiceCreatorOptions Options;
        private SongselectIntegrator ccliIntegrator = new SongselectIntegrator();

        private List<ServiceItem> ServiceItems       = new List<ServiceItem>();
        private Dictionary<string, string> SongsDict = new Dictionary<string, string>();
        private Dictionary<string, string> PPTDict   = new Dictionary<string, string>();
        private List<ServiceItem> MediaItems         = new List<ServiceItem>();

        public string ErrorLabel       { get; private set; }
        public string CreateButtonText { get; private set; }
        public string SongsInput       { get; private set; }
        #endregion


        #region Initialization
        public ServiceCreator(string createButtonText)
        {
            this.CreateButtonText = createButtonText;
            this.ErrorLabel = "";
            InitSongDictionaries(false);
        }

        public void Init(ServiceCreatorOptions options, string songsInput)
        {
            // Ggf Prüfungen durchführen

            // Daten übernehmen
            this.SongsInput = songsInput;
            this.Options = options;
            this.IsInitialized = true;

            if (Options.PptAsSecondChance) { InitSongDictionaries(true); }
            if (Options.UseCCLI) { ccliIntegrator.UpdateCCLIDictionary(false); }
        }

        public void ClearMediaFiles()
        {
            ServiceItems.Clear();
        }

        public bool AddMediaFile(string path)
        {
            var extension = Path.GetExtension(path);
            var relativePath = GetFilepathProperFormat(path);

            if (extension == ".pdf") { MediaItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.PDF)); }
            else if (".jpg;.jpeg;.png".Contains(extension)) { MediaItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.Image)); }
            else if ("*.mp3;*.wav".Contains(extension)) { MediaItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.Audio)); }
            else if ("*.wmv;*.mov;*.mp4".Contains(extension)) { MediaItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.Video)); }
            else { return false; }

            return true;
        }

        private void InitSongDictionaries(bool initPPTsElseSngs)
        {
            if ((initPPTsElseSngs && PPTDict.Any()) || (!initPPTsElseSngs && SongsDict.Any())) { return; }

            // Alle Songs einlesen und mit SongTitel -> Full Path speichern
            // Alle Song-Dateien unter "Songbeamer/Lieder" durchgehen, nur .sng-Dateien anschauen - Folien für Vorlagen - Ordner ausschließen
            var files = Directory.GetFiles(initPPTsElseSngs ? Settings.Instance.PPTS_PATH : Settings.Instance.SONGS_PATH, initPPTsElseSngs ? "*.ppt?" : "*.sng", SearchOption.AllDirectories)
                                 .Where(x => !x.StartsWith($"{Settings.Instance.SONGS_PATH}\\{Settings.Instance.TEMPLATE_FILES_FOLDER}")).ToList();

            foreach (var path in files)
            {
                try
                {
                    if (initPPTsElseSngs)
                    {
                        PPTDict.Add(Path.GetFileNameWithoutExtension(path).ToLower().Replace(",", ""), path);
                    }
                    else
                    {
                        SongsDict.Add(Path.GetFileNameWithoutExtension(path).ToLower().Replace(",", ""), path);
                    }
                }
                catch
                {
                    if (!initPPTsElseSngs)
                    {
                        // Datei-Name nicht unique...
                        MessageBox.Show(path, "Doppeltes Lied: " + (initPPTsElseSngs ? "PPTs" : "SNGs"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        #endregion

        #region Create Service Schedule
        public bool CreateServiceSchudule()
        {
            if (!IsInitialized) { return false; }

            // Aus Text ServiceItems extrahieren
            var success = ExtractServiceItems();
            if (!success) { return success; } // Nutzeraktion erforderlich

            // Ablauf speichern und ggf. Songbeamer damit öffnen
            var path = SaveServicePlanToFile();
            success = !string.IsNullOrEmpty(path);

            if (success && Options.OpenSongbeamer)
            {
                Process process = new Process();
                process.StartInfo.Arguments = $"\"{path}\"";

                try
                {
                    var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\SongBeamer.Schedule\shell\open\command", false);
                    var value = key.GetValue("").ToString().Substring(1);
                    process.StartInfo.FileName = value.Substring(0, value.IndexOf("\""));

                    process.Start();
                }
                catch (Exception) { }
            }

            return success;
        }

        private string SaveServicePlanToFile()
        {
            // Dateipfad bestimmen und prüfen, ob Datei bereits existiert
            var filepath = Options.IsForYouth ? $"{Settings.Instance.SERVICES_YOUTH_PATH}\\{GetNextFriday()}_Jugend.col" : GetSavePathFromFileDialog();
            if (File.Exists(filepath))
            {
                if (MessageBox.Show("Ablauf-Datei ist schon vorhanden. Überschreiben?", "Datei ersetzen?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    var cancel = true;
                    if (Options.IsForYouth)
                    {
                        filepath = GetSavePathFromFileDialog();
                        cancel = false;
                        if (File.Exists(filepath) && MessageBox.Show("Ablauf-Datei ist schon vorhanden. Überschreiben?", "Datei ersetzen?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes) { cancel = true; }
                    }

                    if (cancel)
                    {
                        ErrorLabel = "Speichern durch Benutzer abgebrochen.";
                        ServiceItems.Clear();
                        return "";
                    }
                }
            }

            if (string.IsNullOrEmpty(filepath)) { ErrorLabel = "Speichern durch Benutzer abgebrochen."; return ""; }

            // Inhalt erstellen und abspeichern
            var content = BuildServicePlan();

            var sw = new StreamWriter(filepath, false, System.Text.Encoding.UTF8);
            if (sw == null) return "";
            sw.Write(content);
            sw.Flush();

            // Close File
            sw.Close();

            return filepath;
        }

        private string BuildServicePlan()
        {
            // Fehler prüfen
            if (ServiceItems.Count < 1) { ErrorLabel = "Fehler: Es wurde kein Song gefunden."; return ""; }

            // Inhalte zusammenfügen
            var text = "object AblaufPlanItems: TAblaufPlanItems" + ServiceItem.NewLine + "  items = <" + ServiceItem.NewLine;

            // Manipulation 0: Bei normalen Veranstaltungen: Notiz: Läuft Klimakammer?
            if (!Options.IsForYouth)
            {
                var noteKlimakammer = new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("Check 1: Läuft Klimakammer?"), null, ServiceItemType.Note);
                text += noteKlimakammer.ToString();
            }

            // Manipulation 1:  Diashow - Vor dem Gottesdienst
            if (!Options.IsForYouth)
            {
                var diashow = new ServiceItem("Vor dem Gottesdienst anzeigen!", null, ServiceItemType.Diashow);
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image));
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Handy-lautlos.jpg", ServiceItemType.Image));

                text += diashow.ToString();
            }

            // Lieder (.sng oder auch .ppt) an den Anfang
            foreach (var item in ServiceItems)
            {
                text += item.ToString();
            }

            // Mediendateien ans Ende fügen
            // Manipulation 2:  falls normaler Gottesdienst: Notiz ans Ende
            if (MediaItems.Any()) { MediaItems.Insert(0, new ServiceItem("Medien:", null, ServiceItemType.Note)); }

            foreach (var item in MediaItems)
            {
                text += item.ToString();
            }

            // Manipulation 3:  2 Diashows ans Gottesdienst-Ende und Klimakammer-Notiz
            if (!Options.IsForYouth)
            {
                var diashow = new ServiceItem("Outro, falls KEIN Bistro", null, ServiceItemType.Diashow);
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\I_punkt2.png", ServiceItemType.Image));
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image));
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 3", $"{Settings.Instance.IMAGES_PATH}\\I_punkt1.png", ServiceItemType.Image));
                diashow.AddSubItemForDiashow(new ServiceItem("Bild 4", $"{Settings.Instance.IMAGES_PATH}\\I_punkt3.png", ServiceItemType.Image));

                text += diashow.ToString();

                var diashow2 = new ServiceItem("Outro, falls Bistro", null, ServiceItemType.Diashow);
                diashow2.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\I_punkt2.png", ServiceItemType.Image));
                diashow2.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image));
                diashow2.AddSubItemForDiashow(new ServiceItem("Bild 3", $"{Settings.Instance.IMAGES_PATH}\\I_punkt1.png", ServiceItemType.Image));
                diashow2.AddSubItemForDiashow(new ServiceItem("Bild 4", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-M12-Bistro-Abends.jpg", ServiceItemType.Image));
                diashow2.AddSubItemForDiashow(new ServiceItem("Bild 5", $"{Settings.Instance.IMAGES_PATH}\\I_punkt3.png", ServiceItemType.Image));

                text += diashow2.ToString();

                var noteKlimakammerSwitchedOff = new ServiceItem("Check 2: Klimakammer ausgeschaltet?", null, ServiceItemType.Note);
                text += noteKlimakammerSwitchedOff.ToString();
            }

            text = text.Remove(text.Length - ServiceItem.NewLine.Length, ServiceItem.NewLine.Length);
            text += ">" + ServiceItem.NewLine + "end" + ServiceItem.NewLine;

            return text;
        }
        #endregion

        #region Create Service Templates
        // Achtung: Nutzer, der das Programm ausführt, braucht entsprechende Rechte in der Nextcloud, um die Vorlagen überschreiben zu können.
        public bool UpdateServiceTemplates()
        {
            // Inhalt erstellen und abspeichern
            for (var type = ServiceTemplateType.MorningService; type <= ServiceTemplateType.EveningService; type++)
            {
                var content = BuildServiceTemplate(type);

                var sw = new StreamWriter(Settings.Instance.SERVICES_TEMPLATES_PATH + "\\" + ServiceTemplateItemData.GetAttr(type).Filename, false, System.Text.Encoding.UTF8);
                if (sw == null) { return false; }
                sw.Write(content);
                sw.Flush();

                // Close File
                sw.Close();
            }
          
            return true;
        }

        // Achtung: Das ServiceTemplateType.Youth wird hier nicht unterstützt bislang!
        private string BuildServiceTemplate(ServiceTemplateType type)
        {
            ServiceItems.Clear();

            // Ablauf-Datei eröffnen
            var text = "object AblaufPlanItems: TAblaufPlanItems" + ServiceItem.NewLine + "  items = <" + ServiceItem.NewLine;

            // Notiz: Läuft Klimakammer?
            if (Settings.Instance.ADD_KLIMAKAMMER_NOTES) { ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("-> Läuft Klimakammer?"), null, ServiceItemType.Note2)); }

            // Diashow - Vor dem Gottesdienst
            var diashowBeginning = new ServiceItem("Diashow vor Gottesdienst anzeigen", null, ServiceItemType.Diashow);
            diashowBeginning.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Handy-lautlos.jpg", ServiceItemType.Image));
            if (type != ServiceTemplateType.MiddayService) { diashowBeginning.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image)); }
            else { AddTempDiashowImages(ref diashowBeginning); }
            
            ServiceItems.Add(diashowBeginning);


            // Liederbücher bei Morgengodi
            if (type == ServiceTemplateType.MorningService) {
                ServiceItems.Add(new ServiceItem("Liederbuch 1", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 1.sng"), ServiceItemType.Song));
            }
            ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("--- Ansagen ---"), null, ServiceItemType.Note));

            if (type == ServiceTemplateType.MorningService)
            {
                ServiceItems.Add(new ServiceItem("Liederbuch 2", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 2.sng"), ServiceItemType.Song));
                ServiceItems.Add(new ServiceItem("Liederbuch 3", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 3.sng"), ServiceItemType.Song));
                ServiceItems.Add(new ServiceItem("Liederbuch 4", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 4.sng"), ServiceItemType.Song));
            }

            // Textlesung, Predigt und Gebetsanliegen
            ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("Bibel-Textlesung_" + ServiceTemplateItemData.GetAttr(type).Abbreviation), 
                                             SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Bibel-Textlesung_{ServiceTemplateItemData.GetAttr(type).Abbreviation}.sng"), ServiceItemType.Song));
             
            ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("--- Predigt ---"), null, ServiceItemType.Note));

            if (type == ServiceTemplateType.MorningService)
            {
                ServiceItems.Add(new ServiceItem("Liederbuch 5", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 1.sng"), ServiceItemType.Song));
            }

            ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("Gebetsanliegen"), SongSheetOpener.UmlautsUTF8ToUnicode(Settings.Instance.PRAYER_POINTS_PPT_PATH), ServiceItemType.PPT));

            if (type == ServiceTemplateType.MorningService)
            {
                ServiceItems.Add(new ServiceItem("Liederbuch 6", SongSheetOpener.UmlautsUTF8ToUnicode($"{Settings.Instance.TEMPLATE_FILES_FOLDER}\\Liederbuch 1.sng"), ServiceItemType.Song));
            }

            // 2 Diashows ans Gottesdienst-Ende und Klimakammer-Notiz
            if (type == ServiceTemplateType.EveningService)
            {
                var diashowEnd1 = new ServiceItem("Outro, falls KEIN Bistro", null, ServiceItemType.Diashow);
                diashowEnd1.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\I_punkt2.png", ServiceItemType.Image));
                diashowEnd1.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image));
                diashowEnd1.AddSubItemForDiashow(new ServiceItem("Bild 3", $"{Settings.Instance.IMAGES_PATH}\\I_punkt1.png", ServiceItemType.Image));
                diashowEnd1.AddSubItemForDiashow(new ServiceItem("Bild 4", $"{Settings.Instance.IMAGES_PATH}\\I_punkt3.png", ServiceItemType.Image));
                AddTempDiashowImages(ref diashowEnd1);

                ServiceItems.Add(diashowEnd1);
            }

            if (type != ServiceTemplateType.MiddayService)
            {
                var diashowEnd2 = new ServiceItem(type == ServiceTemplateType.EveningService ? "Outro, falls Bistro" : "Diashow nach Gottesdienst anzeigen", null, ServiceItemType.Diashow);
                var imageSnackFilename = type == ServiceTemplateType.MorningService ? "FEGMM-Beamerfolie-M12-Bistro-Morgens.jpg" : "FEGMM-Beamerfolie-M12-Bistro-Abends.jpg";
                diashowEnd2.AddSubItemForDiashow(new ServiceItem("Bild 1", $"{Settings.Instance.IMAGES_PATH}\\I_punkt2.png", ServiceItemType.Image));
                diashowEnd2.AddSubItemForDiashow(new ServiceItem("Bild 2", $"{Settings.Instance.IMAGES_PATH}\\FEGMM-Beamerfolie-Gebet.jpg", ServiceItemType.Image));
                diashowEnd2.AddSubItemForDiashow(new ServiceItem("Bild 3", $"{Settings.Instance.IMAGES_PATH}\\I_punkt1.png", ServiceItemType.Image));
                diashowEnd2.AddSubItemForDiashow(new ServiceItem("Bild 4", $"{Settings.Instance.IMAGES_PATH}\\{imageSnackFilename}", ServiceItemType.Image));
                diashowEnd2.AddSubItemForDiashow(new ServiceItem("Bild 5", $"{Settings.Instance.IMAGES_PATH}\\I_punkt3.png", ServiceItemType.Image));
                AddTempDiashowImages(ref diashowEnd2);

                ServiceItems.Add(diashowEnd2);
            }

            if (Settings.Instance.ADD_KLIMAKAMMER_NOTES && type == ServiceTemplateType.EveningService) { ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode("-> Klimakammer ausgeschaltet?"), null, ServiceItemType.Note2)); }

            // Alle Elemente hintereinander anfügen in Datei
            foreach (var item in ServiceItems)
            {
                text += item.ToString();
            }

            text = text.Remove(text.Length - ServiceItem.NewLine.Length, ServiceItem.NewLine.Length);
            text += ">" + ServiceItem.NewLine + "end" + ServiceItem.NewLine;

            return text;
        }

        private bool AddTempDiashowImages(ref ServiceItem diashow)
        {
            var images = Directory.GetFiles(Settings.Instance.IMAGES_DIASHOW_PATH, "*.jp*g", SearchOption.TopDirectoryOnly).ToList();
            var pngImages = Directory.GetFiles(Settings.Instance.IMAGES_DIASHOW_PATH, "*.png", SearchOption.TopDirectoryOnly).ToList();
            images.AddRange(pngImages);

            // Neueres nehmen
            var count = 1;
            foreach (var image in images)
            {
                var split = Path.GetFileName(image).Split('_');
                var date = DateTime.Today;
                if (split.Count() > 1) { if (!DateTime.TryParse(split[0], out date)) { date = DateTime.Today; } }
                if (DateTime.Today.Subtract(date).Days <= 0) {
                    diashow.AddSubItemForDiashow(new ServiceItem($"Einladung {count}", SongSheetOpener.UmlautsUTF8ToUnicode(image), ServiceItemType.Image)); count++;
                }
            }

            return true;
        }
        #endregion

        #region SongExtraction
        // Hier ist die meiste "Magie" drin
        private bool ExtractServiceItems()
        {
            // Zeilenweise durchgehen, leere Zeilen absichtlich behalten, um bei einem Abbruch alles bis zum aktuellen Lied gelöscht zu haben.
            string[] separator = { "\r\n" };
            var newLineLength = 2;
            var songList = SongsInput.Split(separator, StringSplitOptions.None).ToList();

            // Falscher Separator?
            if (songList.Count == 1) { songList = SongsInput.Split('\n').ToList(); newLineLength = 1; }

            // Zeilen analysieren: Häufige Pattern finden, die auf Extrainfos hindeuten
            var validLines = songList.Where(x => x.Trim().Length > 2).Count();
            var removeBrackets = songList.Where(x => x.Contains("(")).Count() / validLines >= 0.75;
            var removeMinusSeparators = songList.Where(x => x.Contains("-")).Count() / validLines >= 0.75;


            // Zeilenweise durchgehen
            foreach (var song in songList)
            {
                if (!song.Any())
                {
                    if (SongsInput.Length > 1) { SongsInput = SongsInput.Remove(0, newLineLength); }
                    continue;
                }

                // Filtern:  Zahlen und Leerzeilen entfernen
                var cleanedTitle = ExtractSongTitle(song);

                // Ist kein Lied, sonder Hinweis auf Andacht -> Notiz hinzufügen und weiter
                var sermonResult = FindAndRemoveSermonStatement(cleanedTitle, song);
                if (sermonResult.Item1)
                {
                    ServiceItems.Add(new ServiceItem(Options.IsForYouth ? "--- Thema ---" : "--- Predigt ---", null, ServiceItemType.Note));
                    SongsInput = SongsInput.Remove(0, song.Length + newLineLength);
                    continue;
                }

                // Folgendes Lied ist optional: Davor eine Notiz hinzufügen
                var optionalResult = FindAndRemoveOptionalStatement(sermonResult.Item2, song);
                if (optionalResult.Item1) { cleanedTitle = optionalResult.Item2; ServiceItems.Add(new ServiceItem("Optional:", null, ServiceItemType.Note)); }

                // Extra-Daten.
                var intelligentCleanedTitle = RemoveExtraInfos(optionalResult.Item2, removeBrackets, removeMinusSeparators);

                // Dateipfad zu Song herausfinden, ggf. mit Teilstring suchen.
                var searchResult = GetFilepathForSongTitle(intelligentCleanedTitle);
                var relativePath = searchResult.Item2;
                var isPPT = searchResult.Item1;
                if (relativePath == null) { return false; }
                if (relativePath != "")
                {
                    if (isPPT)
                    {
                        ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.PPT));
                    }
                    else
                    {
                        ServiceItems.Add(new ServiceItem(SongSheetOpener.UmlautsUTF8ToUnicode(Path.GetFileNameWithoutExtension(relativePath)), relativePath, ServiceItemType.Song));
                    }
                }

                // Falls Fehler auftreten bis zur aktuellen Abarbeitung alles Löschen
                SongsInput = SongsInput.Remove(0, SongsInput.Length > song.Length ? song.Length + newLineLength : song.Length);
            }

            // Bei Jugend: Ans Ende Jugendprogramm als Bild, falls vorhanden, anhängen
            if (Options.IsForYouth)
            {
                var pathYouthSchedule = GetYouthScheduleImagePath();
                if (string.IsNullOrEmpty(pathYouthSchedule) || !File.Exists(pathYouthSchedule)) { return true; }
                ServiceItems.Add(new ServiceItem("Bild Jugendprogramm", @"..\Abl'#228'ufe\_Jugend\" + Path.GetFileName(pathYouthSchedule), ServiceItemType.Image));
            }

            return true;
        }

        private Tuple<bool, string> FindAndRemoveOptionalStatement(string title, string uncleanedTitle)
        {
            // '(optional)' oder ein '[...]'
            if (title.EndsWith("(optional)")) { return Tuple.Create<bool, string>(true, title.Replace("(optional)", "").TrimEnd()); }
            if (title.EndsWith("optional")) { return Tuple.Create<bool, string>(true, title.Replace("optional", "").TrimEnd()); }
            if (uncleanedTitle.StartsWith("[") && uncleanedTitle.EndsWith("]")) { return Tuple.Create<bool, string>(true, ExtractSongTitle(uncleanedTitle.Substring(1, uncleanedTitle.Length - 2))); }

            return Tuple.Create<bool, string>(false, title);
        }

        private Tuple<bool, string> FindAndRemoveSermonStatement(string songTitle, string uncleanedTitle)
        {
            //  '---', 'Andacht', 'Predigt' oder 'Thema' erkennen
            var title = songTitle.ToLower();
            if (uncleanedTitle.Equals("---") || title.Equals("andacht") || title.Equals("thema") || title.Equals("predigt") ||
                title.Equals("(andacht)") || title.Equals("(thema)") || title.Equals("(predigt)")) { return Tuple.Create<bool, string>(true, ""); }

            return Tuple.Create<bool, string>(false, songTitle);
        }

        private string RemoveExtraInfos(string text, bool allBrackets, bool allMinusSeparators)
        {
            // Klammern sollten zuerst entfernt werden, falls der - als Trennung innerhalb der Klammer verwendet wird
            if (allBrackets && text.LastIndexOf('(') > 0) { text = text.Remove(text.LastIndexOf('('), (text.LastIndexOf(')') > 0 ? text.LastIndexOf(')') : text.Length) - text.LastIndexOf('(') + 1).Trim(); }
            else if (allMinusSeparators) { text = text.Remove(text.LastIndexOf('-')).Trim(); }
            return text;
        }

        private string ExtractSongTitle(string songTitle)
        {
            var title = songTitle.ToLower();

            // Alle vorgestellte Aufzählung oder Buchinfos entfernen
            if (title.StartsWith("blatt")) { title = title.Remove(0, "blatt".Length); }
            if (title.StartsWith("e21") || title.StartsWith("fj"))
            {
                // Alles bis zum 1. Buchstaben nach einem Leerzeichen löschen
                songTitle = songTitle.Substring(3);
            }
            songTitle = songTitle.Substring(GetTitleStart(songTitle)).Trim();

            return songTitle.Length <= 3 ? "" : songTitle;
        }

        private int GetTitleStart(string title)
        {
            if (title.Length > 0 && title[0] == '(') { return 0; }

            for (var i = 0; i < title.Length; i++)
            {
                // Klein-, Großbuchstabe oder Umlaut (http://www.tabelle.info/ascii_zeichen_tabelle.html)
                if ((title[i] >= 'a' && title[i] <= 'z') || (title[i] >= 'A' && title[i] <= 'Z') || (title[i] == 'ü' && title[i] <= 'Ü')) { return i; }
            }

            return 0;
        }

        private Tuple<bool, string> GetFilepathForSongTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) { Tuple.Create<bool, string>(false, ""); }

            // Wenn CCLI bevorzugt werden soll, dies zuerst prüfen
            if (Options.UseCCLI)
            {
                var ccli = 0;
                if (int.TryParse(title, out ccli))
                {
                    if (ccliIntegrator.SongNumberDict.ContainsKey(ccli)) { return Tuple.Create<bool, string>(false, GetFilepathProperFormat(ccliIntegrator.SongNumberDict[ccli], false)); }
                }
            }

            // Nach Titel suchen
            var songTitle = title.ToLower().Replace(",", "");

            if (SongsDict.ContainsKey(songTitle)) { return Tuple.Create<bool, string>(false, GetFilepathProperFormat(SongsDict[songTitle], false)); }

            // Suchen mit Teilstrings, falls nichts gefunden wurde
            var result = SearchForSongWithSubstrings(songTitle, SongsDict, false);
            if (!string.IsNullOrEmpty(result)) { return Tuple.Create<bool, string>(false, result); }

            // Nichts bisher gefunden, als PPT suchen?
            if (Options.PptAsSecondChance)
            {
                result = SearchForSongWithSubstrings(songTitle, PPTDict, true);
                if (!string.IsNullOrEmpty(result))
                {
                    MessageBox.Show($"Der Song '{title}' konnte nur als PPT-Datei gefunden werden!", "Lied nur als PPT vorhanden", MessageBoxButton.OK, MessageBoxImage.Information);
                    return Tuple.Create<bool, string>(true, result);
                }
            }

            ErrorLabel = "Fehler: Oberstes Lied nicht gefunden oder nicht eindeutig. Name überprüfen.";
            CreateButtonText = "Erstellen fortsetzen";

            return Tuple.Create<bool, string>(false, null); ;
        }

        private string SearchForSongWithSubstrings(string songTitle, Dictionary<string, string> dict, bool pptSearch)
        {
            // Exakten Titel nicht gefunden - Geht vielleicht mit Teilwörtern (Anfang weglassen)?
            var words = songTitle.Split(' ');
            var minMulitpleChoiceList = new List<string>();

            for (var skip = 0; skip < words.Length; skip++)
            {
                var result = dict.Keys.Where(x => x.Contains(String.Join(" ", skip > 0 ? words.Skip(skip) : words))).ToList();
                if (result.Count == 1)
                {
                    // Eindeutiges Item gefunden -> Gehe davon aus, dass es das richtige ist.
                    return GetFilepathProperFormat(dict[result.First()], pptSearch);
                }
                else if (result.Count > 0 && (minMulitpleChoiceList.Count == 0 || result.Count < minMulitpleChoiceList.Count))
                {
                    minMulitpleChoiceList = result;
                }
            }

            // Exakten Titel nicht gefunden - Geht vielleicht mit Teilwörtern (Ende weglassen)?
            for (var take = words.Length; take >= 0; take--)
            {
                var result = dict.Keys.Where(x => x.Contains(String.Join(" ", take > 0 ? words.Take(take) : words))).ToList();
                if (result.Count == 1)
                {
                    // Eindeutiges Item gefunden -> Gehe davon aus, dass es das richtige ist.
                    return GetFilepathProperFormat(dict[result.First()], pptSearch);
                }
                else if (result.Count > 0 && (minMulitpleChoiceList.Count == 0 || result.Count < minMulitpleChoiceList.Count))
                {
                    minMulitpleChoiceList = result;
                }
            }

            // Wenn die minimale Mehrdeutigkeit <= 3 ist, den Nutzer wählen lassen
            if (minMulitpleChoiceList.Count <= 5 && minMulitpleChoiceList.Count >= 2)
            {
                var count = 1;
                foreach (var possibleSong in minMulitpleChoiceList)
                {
                    var response = MessageBox.Show($"Der {(pptSearch ? "PPT-" : "")} Song '{songTitle}' ist mehrdeutig! {minMulitpleChoiceList.Count} Möglichkeiten:\r\n\r\n{String.Join("\r\n", minMulitpleChoiceList)}\r\n\r\nIst dies der richtige Song?\r\n{dict[possibleSong]}", $"Mehrdeutiges Lied ({count} von {minMulitpleChoiceList.Count}: {possibleSong})", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);

                    if (response == MessageBoxResult.Yes)
                    {
                        // Eindeutiger Song durch Nutzer gefudnen
                        return GetFilepathProperFormat(dict[possibleSong], pptSearch);
                    }
                    else if (response == MessageBoxResult.Cancel) { return null; }
                    count++;
                }
            }

            return null;
        }
        #endregion

        #region Helper
        private string GetYouthScheduleImagePath()
        {
            var images = Directory.GetFiles(Settings.Instance.SERVICES_YOUTH_PATH, "*.jp*g", SearchOption.TopDirectoryOnly).ToList().Where(x => x.ToLower().Contains("programm"));

            if (images.Count() == 1) { return images.First(); }
            else if (images.Count() > 1)
            {
                var newestDate = DateTime.MinValue;
                var newestPath = "";

                // Neueres nehmen
                foreach (var image in images)
                {
                    if (File.GetLastWriteTime(image) > newestDate) { newestDate = File.GetLastWriteTime(image); newestPath = image; }
                }

                return newestPath;
            }

            return null;
        }

        private string GetNextFriday()
        {
            var today = DateTime.Now;

            var nextFriday = today.Add(TimeSpan.FromDays(today.DayOfWeek == DayOfWeek.Saturday ? 6 : (int)DayOfWeek.Friday - ((int)today.DayOfWeek)));

            return nextFriday.ToString("yyyy-MM-dd");
        }

        private string GetSavePathFromFileDialog()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Settings.Instance.SERVICES_PATH;
            saveFileDialog.Title = "Ablauf speichern unter";
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = "col";
            saveFileDialog.Filter = "Songbeamer Abläufe (*.col)|*.col|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            return "";
        }
        
        private string GetFilepathProperFormat(string path, bool isPPTPath)
        {
            if (isPPTPath)
            {
                return GetFilepathProperFormat(path);
                /*var folder = Path.GetDirectoryName(path);
                folder = folder.Remove(0, folder.LastIndexOf("\\") + 1);
                return $"..\\..\\{folder}\\{SongSheetOpener.UmlautsUTF8ToUnicode(path.Remove(0, PPTS_PATH.Length).Substring(1))}";*/
            }

            return SongSheetOpener.UmlautsUTF8ToUnicode(path.Remove(0, Settings.Instance.SONGS_PATH.Length).Substring(1));
        }

        private string GetFilepathProperFormat(string path)
        {
            if (!path.StartsWith(Settings.Instance.SONGS_PATH))
            {
                // Pfad wird immer Relativ zum Lieder-Ordner erstellt:
                var index = 0;
                for (; index < Math.Min(path.Length, Settings.Instance.SONGS_PATH.Length); index++)
                {
                    if (path[index] != Settings.Instance.SONGS_PATH[index]) { break; }
                }
                var shortened = Settings.Instance.SONGS_PATH.Substring(index);
                var difference = shortened.Count(x => x == '\\') + 1; // Lieder Ordner ist ja nicht enthalten

                var pathPrefix = "";
                for (int i=0; i<difference;i++)
                {
                    pathPrefix += "..\\"; 
                }

                return pathPrefix + SongSheetOpener.UmlautsUTF8ToUnicode(path.Remove(0, index));
            }
            else
            {
                return SongSheetOpener.UmlautsUTF8ToUnicode(path.Remove(0, Settings.Instance.SONGS_PATH.Length).Substring(1));
            }
        }
        #endregion
    }
}