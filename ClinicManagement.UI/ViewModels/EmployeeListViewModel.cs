using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ClinicManagement.Core;
using ClinicManagement.Business;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class EmployeeListViewModel : ViewModelBase
    {
        private readonly IEmployeeService _service;

        public ObservableCollection<Employee> Employees { get; set; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }

        public Action<EmployeeFormViewModel> ShowFormDialog { get; set; }

        public EmployeeListViewModel(IEmployeeService service)
        {
            _service = service;
            Employees = new ObservableCollection<Employee>();

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(p => Edit(p as Employee));
        }

        public void LoadData()
        {
            Employees.Clear();
            var list = _service.GetAllEmployees();
            foreach(var e in list)
            {
                Employees.Add(e);
            }
        }

        private void Add()
        {
            var formVm = new EmployeeFormViewModel(_service);
            formVm.OnSaved += LoadData;
            ShowFormDialog?.Invoke(formVm);
        }

        private void Edit(Employee emp)
        {
            if (emp == null) return;
            var formVm = new EmployeeFormViewModel(_service);
            formVm.LoadEmployee(emp);
            formVm.OnSaved += LoadData;
            ShowFormDialog?.Invoke(formVm);
        }
    }
}