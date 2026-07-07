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
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class SalaryListViewModel : ViewModelBase
    {
        private readonly ISalaryService _salaryService;

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged(nameof(SelectedMonth));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<SalaryRecord> SalaryRecords { get; set; }

        public ICommand CalculateCommand { get; }
        public ICommand FinalizeCommand { get; }

        public SalaryListViewModel()
        {
            var context = new ClinicDbContext();
            var unitOfWork = new UnitOfWork(context);
            _salaryService = new SalaryService(unitOfWork);

            SelectedMonth = DateTime.Now.Month;
            SelectedYear = DateTime.Now.Year;

            SalaryRecords = new ObservableCollection<SalaryRecord>();

            CalculateCommand = new RelayCommand(param => Calculate());
            FinalizeCommand = new RelayCommand(param => FinalizePayroll(), param => CanFinalize());

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var records = _salaryService.GetSalaryRecords(SelectedMonth, SelectedYear);
                SalaryRecords.Clear();
                foreach (var r in records)
                {
                    SalaryRecords.Add(r);
                }
                OnPropertyChanged(nameof(SalaryRecords));
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Calculate()
        {
            try
            {
                var records = _salaryService.CalculatePayroll(SelectedMonth, SelectedYear);
                SalaryRecords.Clear();
                foreach (var r in records)
                {
                    SalaryRecords.Add(r);
                }
                CommandManager.InvalidateRequerySuggested();
                MessageBox.Show("Tính lương thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tính lương: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanFinalize()
        {
            if (SelectedMonth < 1 || SelectedMonth > 12 || SelectedYear < 1)
                return false;

            var payrollMonth = new DateTime(SelectedYear, SelectedMonth, 1);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            return SalaryRecords.Any()
                && payrollMonth < currentMonth
                && !SalaryRecords.Any(r => r.IsFinalized)
                && !SalaryRecords.Any(r => r.Employee == null || string.IsNullOrWhiteSpace(r.Employee.FullName));
        }

        private void FinalizePayroll()
        {
            var result = MessageBox.Show("Sau khi chốt lương, bảng lương sẽ bị khóa và công thức tính lương tại thời điểm này sẽ được lưu lại (Snapshot). Bạn có chắc chắn không?", 
                "Xác nhận chốt lương", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _salaryService.FinalizePayroll(SelectedMonth, SelectedYear);
                    MessageBox.Show("Chốt lương thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi chốt lương: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
