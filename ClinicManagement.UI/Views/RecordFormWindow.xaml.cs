using System.Windows;

namespace ClinicManagement.UI.Views
{
    public partial class RecordFormWindow : Window
    {
        public RecordFormWindow()
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
