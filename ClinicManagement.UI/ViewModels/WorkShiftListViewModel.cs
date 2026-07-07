using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Business.Services;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.UI.Views;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class WorkShiftListViewModel : ViewModelBase
    {
        private readonly IWorkShiftService _workShiftService;
        private ObservableCollection<WorkShift> _workShifts;
        private WorkShift _selectedWorkShift;

        public ObservableCollection<WorkShift> WorkShifts
        {
            get => _workShifts;
            set { _workShifts = value; OnPropertyChanged(nameof(WorkShifts)); }
        }

        public WorkShift SelectedWorkShift
        {
            get => _selectedWorkShift;
            set { _selectedWorkShift = value; OnPropertyChanged(nameof(SelectedWorkShift)); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public WorkShiftListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _workShiftService = new WorkShiftService(unitOfWork);

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(), param => SelectedWorkShift != null);
            DeleteCommand = new RelayCommand(param => Delete(), param => SelectedWorkShift != null);

            LoadData();
        }

        private void LoadData()
        {
            var shifts = _workShiftService.GetAllWorkShifts();
            WorkShifts = new ObservableCollection<WorkShift>(shifts);
        }

        private void Add()
        {
            var formVm = new WorkShiftFormViewModel();
            var formWindow = new WorkShiftFormWindow { DataContext = formVm };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Edit()
        {
            if (SelectedWorkShift == null) return;
            var formVm = new WorkShiftFormViewModel(SelectedWorkShift);
            var formWindow = new WorkShiftFormWindow { DataContext = formVm };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Delete()
        {
            if (SelectedWorkShift == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa ca làm việc '{SelectedWorkShift.Name}' không?", 
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _workShiftService.DeleteWorkShift(SelectedWorkShift.Id);
                    LoadData();
                    MessageBox.Show("Xóa ca làm việc thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
