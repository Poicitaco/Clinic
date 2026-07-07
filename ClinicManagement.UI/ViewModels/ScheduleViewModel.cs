using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Business.Services;
using ClinicManagement.Business;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class ScheduleViewModel : ViewModelBase
    {
        private readonly IWorkShiftService _workShiftService;
        private readonly IEmployeeService _employeeService;

        private DateTime _currentWeekStart;
        public DateTime CurrentWeekStart
        {
            get => _currentWeekStart;
            set
            {
                _currentWeekStart = value;
                OnPropertyChanged(nameof(CurrentWeekStart));
                OnPropertyChanged(nameof(WeekDisplayTitle));
                LoadSchedule();
            }
        }

        public string WeekDisplayTitle => $"Tuần từ {CurrentWeekStart:dd/MM/yyyy} đến {CurrentWeekStart.AddDays(6):dd/MM/yyyy}";

        public ObservableCollection<EmployeeScheduleRow> EmployeeRows { get; set; } = new ObservableCollection<EmployeeScheduleRow>();
        public List<WorkShift> AllShifts { get; set; }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ClonePreviousWeekCommand { get; }

        public ScheduleViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _workShiftService = new WorkShiftService(unitOfWork);
            _employeeService = new EmployeeService(unitOfWork);

            PreviousWeekCommand = new RelayCommand(param => CurrentWeekStart = CurrentWeekStart.AddDays(-7));
            NextWeekCommand = new RelayCommand(param => CurrentWeekStart = CurrentWeekStart.AddDays(7));
            SaveCommand = new RelayCommand(param => Save());
            ClonePreviousWeekCommand = new RelayCommand(param => ClonePreviousWeek());

            // Get Monday of the current week
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            CurrentWeekStart = today.AddDays(-1 * diff).Date;
        }

        private void LoadSchedule()
        {
            try
            {
                AllShifts = _workShiftService.GetAllWorkShifts();
                var employees = _employeeService.GetAllEmployees().Where(e => e.ContractStatus == ContractStatus.Working).ToList();
                var existingSchedules = _workShiftService.GetSchedulesByDateRange(CurrentWeekStart, CurrentWeekStart.AddDays(6));

                EmployeeRows.Clear();
                foreach (var emp in employees)
                {
                    var row = new EmployeeScheduleRow { Employee = emp };
                    for (int i = 0; i < 7; i++)
                    {
                        var currentDate = CurrentWeekStart.AddDays(i);
                        var cell = new DailyScheduleCell { Date = currentDate };

                        foreach (var shift in AllShifts)
                        {
                            bool isSelected = existingSchedules.Any(s => s.EmployeeId == emp.Id && s.ScheduleDate == currentDate && s.WorkShiftId == shift.Id);
                            cell.Shifts.Add(new ShiftSelection { Shift = shift, IsSelected = isSelected });
                        }
                        row.Days.Add(cell);
                    }
                    EmployeeRows.Add(row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save()
        {
            try
            {
                var schedulesToSave = new List<EmployeeSchedule>();
                foreach (var row in EmployeeRows)
                {
                    foreach (var day in row.Days)
                    {
                        foreach (var shiftSelection in day.Shifts.Where(s => s.IsSelected))
                        {
                            schedulesToSave.Add(new EmployeeSchedule
                            {
                                EmployeeId = row.Employee.Id,
                                ScheduleDate = day.Date,
                                WorkShiftId = shiftSelection.Shift.Id
                            });
                        }
                    }
                }

                _workShiftService.SaveWeeklySchedules(schedulesToSave, CurrentWeekStart, CurrentWeekStart.AddDays(6));
                MessageBox.Show("Lưu lịch thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSchedule(); // Reload to reflect any potential server-side changes
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu lịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClonePreviousWeek()
        {
            var result = MessageBox.Show($"Bạn có chắc muốn sao chép lịch từ tuần trước sang tuần này không? Hành động này sẽ thay thế các lịch hiện tại trong tuần.", 
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _workShiftService.ClonePreviousWeekSchedules(CurrentWeekStart);
                    MessageBox.Show("Sao chép lịch thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSchedule();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class EmployeeScheduleRow
    {
        public Employee Employee { get; set; }
        public ObservableCollection<DailyScheduleCell> Days { get; set; } = new ObservableCollection<DailyScheduleCell>();
    }

    public class DailyScheduleCell : ViewModelBase
    {
        public DateTime Date { get; set; }
        public ObservableCollection<ShiftSelection> Shifts { get; set; } = new ObservableCollection<ShiftSelection>();
    }

    public class ShiftSelection : ViewModelBase
    {
        public WorkShift Shift { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }
    }
}
