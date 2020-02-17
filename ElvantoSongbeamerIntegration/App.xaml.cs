﻿using ElvantoSongbeamerIntegration.Controller;
using ElvantoSongbeamerIntegration.Model;
using SongbeamerSongbookIntegrator;
using System.Threading.Tasks;
using System.Windows;

namespace ElvantoSongbeamerIntegration
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Test ElvantoAPI
            //var elvantoClient = new ElvantoApi.Client("JcBNUpCE9Acygs95SnC4kgbryqPmurCZ");

            //var result = elvantoClient.ServicesGetAllAsync();


            // Wenn genau 1 Argument übergeben wurde, gewünschte Aktion ausführen und dann Programm ohne GUI beenden
            if (e.Args.Length == 1)
            {
                if (e.Args[0][0] == '#')
                {
                    var servicePath = SongSheetOpener.FindCurrentlyOpenedService(e.Args[0].Remove(0, 1));
                    TestSongbookExtractionAsync(TaskTypes.openSheetsService, servicePath);
                }
                else if (e.Args[0][0] == '$')
                {
                    TestSongbookExtractionAsync(TaskTypes.findDuplicateSongs);
                }
                else if (e.Args[0][0] == '§')
                {
                    TestSongbookExtractionAsync(TaskTypes.findDuplicateCCLIs);
                }
                else
                {
                    TestSongbookExtractionAsync(TaskTypes.openSheetSong, e.Args[0]);
                }

                Application.Current.Shutdown();
                return;
            }

            // Sonst GUI zum Zusammenstellen eines Ablaufs aus Liedern anzeigen (MainWindow.xaml)  -> Properties -> Debug -> Befehlszeilenargumente
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task TestSongbookExtractionAsync(TaskTypes taskType, string arg = null)
        {
            var songbookImporter = new SongbookImporter(Settings.Instance.E21_SONGS_PATH);
            var songSheetOpener = new SongSheetOpener(Settings.Instance.E21_SONGS_PATH, Settings.Instance.URL_EXCEPTIONS_PATH);
            var ccliIntegrator = new SongselectIntegrator();
            var logString = "";

            switch (taskType)
            {
                case TaskTypes.importSongbooksToAllSongs:
                    await songbookImporter.AddSonbooksToFilesInFolder(Settings.Instance.SONGS_PATH, false, true);
                    break;

                case TaskTypes.importSongbookOneSong:
                    await songbookImporter.AddSongbooksToFile(arg ?? @"C:\Lieder\Amazing Grace.sng", false);
                    break;

                case TaskTypes.openSheetSong:

                    logString = songSheetOpener.OpenSongInElvantoIfExists(arg != null ? System.IO.Path.GetFileNameWithoutExtension(arg) : @"All ihr Geschöpfe unsres Herrn", arg ?? @"C:\Lieder\Liederpool\All ihr Geschöpfe unsres Herrn.sng");
                    if (!logString.Contains("Elvanto")) { MessageBox.Show(logString, "Lieder in Elvanto geöffnet", MessageBoxButton.OK, MessageBoxImage.Information); }
                    break;

                case TaskTypes.openSheetsService:
                    logString = songSheetOpener.ParseService(arg ?? @"C:\Nextcloud\Medientechnik\SongBeamer\Abläufe\_Jugend\2020-01-10_Jugend.col", Settings.Instance.SONGS_PATH);
                    MessageBox.Show(logString, "Lieder von zuletzt gespeichertem Ablauf geöffnet", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case TaskTypes.findDuplicateSongs:
                    logString = string.Join("\r\n", songSheetOpener.FindSongDuplicates());
                    MessageBox.Show(string.IsNullOrEmpty(logString) ? "Keine Duplikate gefunden :)" : logString, "Doppelte Lieder", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case TaskTypes.findDuplicateCCLIs:
                    logString = string.Join("\r\n", ccliIntegrator.FindCCLIDuplicates());
                    MessageBox.Show(string.IsNullOrEmpty(logString) ? "Keine Duplikate gefunden :)" : logString, "Doppelte CCLI-Nummern", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }

            //var result = await songbookImporter.GetSongbooksOfSong("Amazing Grace");

            //
        }
    }
}
