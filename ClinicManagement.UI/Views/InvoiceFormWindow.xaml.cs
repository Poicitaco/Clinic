using System.Windows;

namespace ClinicManagement.UI.Views
{
    public partial class InvoiceFormWindow : Window
    {
        public InvoiceFormWindow()
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
