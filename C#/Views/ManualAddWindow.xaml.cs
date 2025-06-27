using System.Windows;

namespace NexusApp.Views
{
    /// <summary>
    /// Interaction logic for ManualAddWindow.xaml
    /// </summary>
    public partial class ManualAddWindow : Window
    {
        public ManualAddWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
