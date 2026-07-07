using System.Windows;

namespace ClinicManagement.UI.Views
{
    public partial class ServiceFormWindow : Window
    {
        public ServiceFormWindow()
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
