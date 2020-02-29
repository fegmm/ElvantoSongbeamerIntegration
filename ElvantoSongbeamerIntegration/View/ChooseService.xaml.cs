﻿using ElvantoSongbeamerIntegration.Controller;
using SongbeamerSongbookIntegrator.Controller;
using SongbeamerSongbookIntegrator.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            if (!datePicker.SelectedDate.HasValue) { errorLabel.Content = "Bitte Datum auswählen"; }

            var serviceType = (ServiceTemplateType)serviceListBox.SelectedIndex;
            var dateAndTime = datePicker.SelectedDate.Value.AddHours(Services[((string)serviceListBox.SelectedItem)]);

            Integrator.CreateScheduleForService(dateAndTime, serviceType).ConfigureAwait(false);
            errorLabel.Content = Integrator.Errors;
        }
        #endregion
    }
}