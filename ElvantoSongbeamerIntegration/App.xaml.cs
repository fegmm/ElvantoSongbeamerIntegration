using ElvantoApi.Models;
using ElvantoSongbeamerIntegration.Controller;
using ElvantoSongbeamerIntegration.Model;
using ElvantoSongbeamerIntegration.View;
using SongbeamerSongbookIntegrator;
using System;
using System.Threading.Tasks;
using System.Windows;

// Hinweise bei Problemen mit Mischung von synchronem und asynchromen Code: https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md

namespace ElvantoSongbeamerIntegration
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Wenn genau 1 Argument übergeben wurde, gewünschte Aktion ausführen und dann Programm ohne GUI beenden
            if (e.Args.Length == 1)
            {
                if (e.Args[0][0] == '#')
                {
                    var servicePath = SongSheetOpener.FindCurrentlyOpenedService(e.Args[0].Remove(0, 1));
                    TestSongbookExtractionAsync(TaskType.openSheetsService, servicePath).Wait();
                }
                else if (e.Args[0][0] == '$')
                {
                    TestSongbookExtractionAsync(TaskType.findDuplicateSongs).Wait();
                }
                else if (e.Args[0][0] == '§')
                {
                    TestSongbookExtractionAsync(TaskType.findDuplicateCCLIs).Wait();
                }
                else if (e.Args[0][0] == '%')
                {
                    TestSongbookExtractionAsync(TaskType.updateServiceTemplates).Wait();
                }
                else
                {
                    TestSongbookExtractionAsync(TaskType.openSheetSong, e.Args[0]).Wait();
                }

                Application.Current.Shutdown();
                return;
            }

            /*TestSongbookExtractionAsync(TaskType.importSongbooksToAllSongs).Wait();
            Application.Current.Shutdown();
            return;*/
            
            // Sonst GUI zum Zusammenstellen eines Ablaufs aus Liedern anzeigen (MainWindow.xaml)  -> Properties -> Debug -> Befehlszeilenargumente
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task TestSongbookExtractionAsync(TaskType taskType, string arg = null)
        {
            var songbookImporter = new SongbookImporter(Settings.Instance.E21_SONGS_PATH);
            var songSheetOpener = new SongSheetOpener(Settings.Instance.E21_SONGS_PATH, Settings.Instance.URL_EXCEPTIONS_PATH);
            var ccliIntegrator = new SongselectIntegrator();
            var serviceCreator = new ServiceCreator("", true);
            var logString = "";

            switch (taskType)
            {
                case TaskType.importSongbooksToAllSongs:
                    await songbookImporter.AddSonbooksToFilesInFolder(Settings.Instance.SONGS_PATH, false, true);
                    break;

                case TaskType.importSongbookOneSong:
                    await songbookImporter.AddSongbooksToFile(arg ?? @"C:\Lieder\Amazing Grace.sng", false);
                    break;

                case TaskType.openSheetSong:

                    logString = songSheetOpener.OpenSongInElvantoIfExists(arg != null ? System.IO.Path.GetFileNameWithoutExtension(arg) : @"All ihr Geschöpfe unsres Herrn", arg ?? @"C:\Lieder\Liederpool\All ihr Geschöpfe unsres Herrn.sng");
                    if (!logString.Contains("Elvanto")) { MessageBox.Show(logString, "Lieder in Elvanto geöffnet", MessageBoxButton.OK, MessageBoxImage.Information); }
                    break;

                case TaskType.openSheetsService:
                    logString = songSheetOpener.ParseService(arg ?? @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend\2020-01-10_Jugend.col", Settings.Instance.SONGS_PATH);
                    MessageBox.Show(logString, "Lieder von zuletzt gespeichertem Ablauf geöffnet", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case TaskType.findDuplicateSongs:
                    logString = string.Join("\r\n", songSheetOpener.FindSongDuplicates());
                    MessageBox.Show(string.IsNullOrEmpty(logString) ? "Keine Duplikate gefunden :)" : logString, "Doppelte Lieder", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case TaskType.findDuplicateCCLIs:
                    logString = string.Join("\r\n", ccliIntegrator.FindCCLIDuplicates());
                    MessageBox.Show(string.IsNullOrEmpty(logString) ? "Keine Duplikate gefunden :)" : logString, "Doppelte CCLI-Nummern", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case TaskType.updateServiceTemplates:
                    if (!serviceCreator.UpdateServiceTemplates())
                    {
                        MessageBox.Show("Beim Aktualisieren der Gottesdienst-Vorlagen ist ein nicht näher spezifizierter Fehler aufgetreten!", "Fehler beim Vorlagen-Aktualisieren", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Die Gottesdienst-Vorlagen wurden erfolgreich aktualisiert.", "Vorlagen aktualisiert", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
            }

            //var result = await songbookImporter.GetSongbooksOfSong("Amazing Grace");

            //
        }
    }
}
