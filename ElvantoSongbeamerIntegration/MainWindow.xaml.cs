using Microsoft.Win32;
using ElvantoSongbeamerIntegration.Controller;
using System.Windows;
using System.Windows.Media;
using ElvantoSongbeamerIntegration.Model;
using SongbeamerSongbookIntegrator;

namespace ElvantoSongbeamerIntegration
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceCreator ServiceCreator;


        public MainWindow()
        {
            InitializeComponent();

            ServiceCreator = new ServiceCreator((string)buttonCreate.Content);
        }

        private void Create_Service_Clicked(object sender, RoutedEventArgs e)
        {
            var options = new ServiceCreatorOptions() {
                IsForYouth             = optionForYouth.IsChecked ?? false,
                OpenSongbeamer         = optionOpenSongbeamer.IsChecked ?? false,
                PptAsSecondChance      = optionAlsoPPTs.IsChecked ?? false,
                RecognizeOptionalSongs = optionOptionalSongs.IsChecked ?? false,
                RecognizeSermon        = optionSermonIncluded.IsChecked ?? false,
                UseCCLI                = optionUseCCLI.IsChecked ?? false
            }; 

            // Inhalte prüfen
            errorLabel.Content = "";
            if (songsInput.Text.Length < 4) { errorLabel.Content = "Fehler: Bitte Songs angeben."; return; }

            // Optionen an ServiceCreator übergeben
            ServiceCreator.Init(options, songsInput.Text);

            // Aus Input und Optionen, falls möglich, Ablauf erstellen
            var success = ServiceCreator.CreateServiceSchudule();

            // Labels setzen
            errorLabel.Foreground = new SolidColorBrush(Colors.Red);
            errorLabel.Content   = ServiceCreator.ErrorLabel;
            buttonCreate.Content = ServiceCreator.CreateButtonText;
            songsInput.Text      = ServiceCreator.SongsInput;

            // Anwendung bei Erfolg schließen
            if (success) { Application.Current.Shutdown(); }
        }

        /*private string DecodeDiashow()
        {
            var text = @"6F626A6563742050726573656E746174696F6E536C69646553686F773A205450726573656E746174696F6E536C69646553686F770D0A2020536C696465436F6C6C656374696F6E203D203C0D0A202020206974656D0D0A20202020202046696C654E616D65203D200D0A202020202020202027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C4645474D4D2D4265616D6572666F6C69652D4727202B0D0A202020202020202027656265742E6A7067270D0A20202020656E640D0A202020206974656D0D0A20202020202046696C654E616D65203D200D0A202020202020202027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C4645474D4D2D4265616D6572666F6C69652D4827202B0D0A202020202020202027616E64792D6C6175746C6F732E6A7067270D0A20202020656E643E0D0A20204C6F6F70203D20547275650D0A2020466974546F53637265656E203D2046616C73650D0A656E640D0A";
            var text2 = "9166F4F7-4AA1-46E6-947D-892DEA04554B";//@"6F626A6563742050726573656E746174696F6E536C69646553686F773A205450726573656E746174696F6E536C69646553686F770D0A2020536C696465436F6C6C656374696F6E203D203C0D0A202020206974656D0D0A20202020202046696C654E616D65203D2027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C495F70756E6B74322E706E67270D0A20202020656E640D0A202020206974656D0D0A20202020202046696C654E616D65203D200D0A202020202020202027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C4645474D4D2D4265616D6572666F6C69652D4727202B0D0A202020202020202027656265742E6A7067270D0A20202020656E640D0A202020206974656D0D0A20202020202046696C654E616D65203D2027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C495F70756E6B74312E706E67270D0A20202020656E640D0A202020206974656D0D0A20202020202046696C654E616D65203D2027433A5C4E657874636C6F75645C4D656469656E746563686E696B5C536F6E674265616D65725C42696C6465725C495F70756E6B74332E706E67270D0A20202020656E643E0D0A20204C6F6F70203D20547275650D0A2020466974546F53637265656E203D2046616C73650D0A656E640D0A";

            var encoded = ConvertHex(text);
            var encoded2 = ConvertHex(text2);

            var decoded = ASCIItoHex(encoded);
            var decoded2 = ASCIItoHex(encoded2);

            if (decoded2.Equals(text2)) { return "Toll"; }

            return "Ok";
        }*/

        

        private void buttonIncludeMedia_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Alle Medien-Dateien|*.pdf;*.wmv;*.mov;*.mp4;*.jpg;*.jpeg;*.png;*.mp3;*.wav|PDF-Dateien (*.pdf)|*.pdf|Bilder (*.jpg, *.png)|*.jpg;*.jpeg;*.png|Audio-Dateien (*.mp3, *.wav)|*.mp3;*.wav|Videos (*.wmv, *.mov, *mp4)|*.wmv;*.mov;*.mp4";
            openFileDialog.InitialDirectory = optionForYouth.IsChecked == true ? Settings.Instance.SERVICES_YOUTH_PATH : Settings.Instance.SERVICES_PATH;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var path in openFileDialog.FileNames)
                {
                    ServiceCreator.AddMediaFile(path);
                }
                errorLabel.Content = $"Es wurde(n) {openFileDialog.FileNames.Length} Medien-Datei(en) ans Ende des Ablaufs angefügt.";
                errorLabel.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void buttonDeleteMedia_Click(object sender, RoutedEventArgs e)
        {
            ServiceCreator.ClearMediaFiles();
            errorLabel.Content = $"Es wurden alle Medien-Dateien aus dem Ablauf gelöscht.";
            errorLabel.Foreground = new SolidColorBrush(Colors.Green);
        }
    }
}