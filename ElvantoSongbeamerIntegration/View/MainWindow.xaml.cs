using SongbeamerSongbookIntegrator.View;
using System.Windows;

namespace ElvantoSongbeamerIntegration.View
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pageNavigation.NavigationService.Navigate(new CreateService());
        }
    }
}