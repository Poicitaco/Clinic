using System;
using System.Windows;
using ClinicManagement.UI.ViewModels;

namespace ClinicManagement.UI.Views
{
    public partial class EmployeeFormWindow : Window
    {
        public EmployeeFormWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
