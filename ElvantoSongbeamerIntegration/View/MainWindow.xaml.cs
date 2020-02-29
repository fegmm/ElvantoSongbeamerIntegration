using SongbeamerSongbookIntegrator.View;
using System.Windows;

namespace ElvantoSongbeamerIntegration.View
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool StartWithElvantoIntegrator;

        public MainWindow(bool startElvantoIntegrator)
        {
            StartWithElvantoIntegrator = startElvantoIntegrator;

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {           
            if (StartWithElvantoIntegrator) { pageNavigation.NavigationService.Navigate(new ChooseService()); }
            else { pageNavigation.NavigationService.Navigate(new CreateService()); }
        }
    }
}