using System.Windows;
using ClinicManagement.UI.ViewModels;

namespace ClinicManagement.UI.Views
{
    public partial class WorkShiftFormWindow : Window
    {
        public WorkShiftFormWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is WorkShiftFormViewModel vm)
                {
                    vm.CloseAction = (result) =>
                    {
                        DialogResult = result;
                        Close();
                    };
                }
            };
        }
    }
}
