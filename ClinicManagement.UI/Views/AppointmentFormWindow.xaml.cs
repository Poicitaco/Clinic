using System.Windows;

namespace ClinicManagement.UI.Views
{
    public partial class AppointmentFormWindow : Window
    {
        public AppointmentFormWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
