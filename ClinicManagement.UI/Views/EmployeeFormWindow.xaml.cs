using System;
using System.Windows;
using ClinicManagement.UI.ViewModels;

namespace ClinicManagement.UI.Views
{
    public partial class EmployeeFormWindow : Window
    {
        private EmployeeFormViewModel _viewModel;

        public EmployeeFormWindow()
        {
            InitializeComponent();
            DataContextChanged += EmployeeFormWindow_DataContextChanged;
        }

        private void EmployeeFormWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= Close;
            }

            _viewModel = DataContext as EmployeeFormViewModel;
            if (_viewModel != null)
            {
                _viewModel.RequestClose += Close;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= Close;
            }

            base.OnClosed(e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
