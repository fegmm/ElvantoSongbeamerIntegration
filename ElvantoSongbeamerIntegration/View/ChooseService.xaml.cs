using ElvantoSongbeamerIntegration.Controller;
using SongbeamerSongbookIntegrator.Controller;
using SongbeamerSongbookIntegrator.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SongbeamerSongbookIntegrator.View
{
    /// <summary>
    /// Interaktionslogik für ChooseService.xaml
    /// </summary>
    public partial class ChooseService : Page
    {
        #region Variablen
        private ElvantoIntegrator Integrator;

        // Date ist die Zeitzone UTC-0, man muss also -1 Stunde draufrechnen.
        private readonly Dictionary<string, double> Services = new Dictionary<string, double>() { { "Morgens", 10 }, { "Mittags", 12 }, { "Abends", 19 } };
        #endregion

        #region Initialization
        public ChooseService()
        {
            InitializeComponent();

            foreach (var item in Services.Keys)
            {
                serviceListBox.Items.Add(item);
            }

            datePicker.DisplayDate = ServiceCreator.GetNextSunday();
            datePicker.Text = datePicker.DisplayDate.ToString("dd.MM.yyyy");
            //datePicker.BlackoutDates = new CalendarBlackoutDatesCollection()

            buttonStart.IsEnabled = false;

            Init().ConfigureAwait(false);
        }

        private async Task<bool> Init()
        {
            Integrator = new ElvantoIntegrator();
            await Integrator.Init();

            buttonStart.IsEnabled = true;

            return true;
        }
        #endregion

        #region GUI Interaction
        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            errorLabel.Foreground = new SolidColorBrush(Colors.Red);
            errorLabel.Content = "";

            if (!datePicker.SelectedDate.HasValue) { errorLabel.Content = "Bitte Datum auswählen"; }

            var serviceType = (ServiceTemplateType)serviceListBox.SelectedIndex;
            var dateAndTime = datePicker.SelectedDate.Value.AddHours(Services[((string)serviceListBox.SelectedItem)]);
            var openSongbeamer = optionOpenSongbeamer.IsChecked ?? false;

            Integrator.IntegrationFinishedEvent += IntegrationFinished;
            Integrator.CreateScheduleForService(dateAndTime, serviceType, openSongbeamer).ConfigureAwait(false);
        }

        private void changeToServiceCreator_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CreateService());
        }

        private void IntegrationFinished(object sender, EventArgs e)
        {
            errorLabel.Content = Integrator.Errors;

            if (string.IsNullOrEmpty(Integrator.Errors)) 
            {
                if (!string.IsNullOrEmpty(Integrator.Warnings))
                {
                    errorLabel.Foreground = new SolidColorBrush(Colors.Yellow);
                    errorLabel.Content = Integrator.Warnings;
                }
                else
                {
                    errorLabel.Foreground = new SolidColorBrush(Colors.Green);
                    errorLabel.Content = "Der Ablauf wurde erfolgreich erstellt.";
                }
            }
        }

        #endregion

        private void updateCCLI_Click(object sender, RoutedEventArgs e)
        {
            var songselectIntegrator = new SongselectIntegrator();
            var result = songselectIntegrator.UpdateCCLIDictionary(true);

            errorLabel.Foreground = new SolidColorBrush(result ? Colors.Green : Colors.Red);
            errorLabel.Content = "CCLI-Datenbank " + (result ? "" : "nicht") + " erfolgreich aktualisiert.";
        }

        private void serviceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (datePicker.SelectedDate == null) { return; }

            var serviceType = (ServiceTemplateType)serviceListBox.SelectedIndex;
            var dateAndTime = datePicker.SelectedDate.Value.AddHours(Services[((string)serviceListBox.SelectedItem)]);

            CheckBistroAsync(dateAndTime, serviceType).ConfigureAwait(false);
        }

        public async Task<bool> CheckBistroAsync(DateTime dateTime, ServiceTemplateType serviceType)
        {
            var isBistro = await Integrator?.HasServiceBistro(dateTime, serviceType);
            imageBistro.Visibility = isBistro ? Visibility.Visible : Visibility.Hidden;

            return true;
        }

        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => serviceListBox_SelectionChanged(sender, e);
    }
}
