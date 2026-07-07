using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class PersonalScheduleViewModel : ViewModelBase
    {
        private DateTime _weekStart;
        private string _emptyMessage;

        public ObservableCollection<PersonalScheduleRow> ScheduleRows { get; }

        public string WeekRangeText => $"Tuần từ {_weekStart:dd/MM/yyyy} đến {_weekStart.AddDays(6):dd/MM/yyyy}";

        public string EmptyMessage
        {
            get => _emptyMessage;
            set => SetProperty(ref _emptyMessage, value);
        }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }

        public PersonalScheduleViewModel()
        {
            UserContext.CheckRole(EmployeeRole.Dentist, EmployeeRole.Receptionist);

            _weekStart = GetWeekStart(DateTime.Today);
            ScheduleRows = new ObservableCollection<PersonalScheduleRow>();
            PreviousWeekCommand = new RelayCommand(_ => ChangeWeek(-7));
            NextWeekCommand = new RelayCommand(_ => ChangeWeek(7));

            LoadData();
        }

        private void ChangeWeek(int days)
        {
            _weekStart = _weekStart.AddDays(days);
            OnPropertyChanged(nameof(WeekRangeText));
            LoadData();
        }

        private void LoadData()
        {
            ScheduleRows.Clear();

            var currentUser = UserContext.CurrentUser;
            var weekEnd = _weekStart.AddDays(6);

            using (var context = new ClinicDbContext())
            {
                var schedules = context.EmployeeSchedules
                    .Include(s => s.WorkShift)
                    .Where(s => s.EmployeeId == currentUser.Id
                        && s.ScheduleDate >= _weekStart
                        && s.ScheduleDate <= weekEnd)
                    .OrderBy(s => s.ScheduleDate)
                    .ThenBy(s => s.WorkShift.StartTime)
                    .ToList();

                foreach (var schedule in schedules)
                {
                    var shift = schedule.WorkShift;
                    var shiftHours = shift?.TotalHours ?? 0m;
                    var shiftCoefficient = (decimal)(schedule.ShiftCoefficient ?? 1.0f);
                    var patientCoefficient = (decimal)schedule.PatientCoefficient;

                    ScheduleRows.Add(new PersonalScheduleRow
                    {
                        ScheduleDate = schedule.ScheduleDate,
                        ShiftName = shift?.Name,
                        StartTime = shift?.StartTime,
                        EndTime = shift?.EndTime,
                        ShiftHours = shiftHours,
                        ShiftCoefficient = shiftCoefficient,
                        PatientCoefficient = patientCoefficient,
                        ConvertedHours = shiftHours * (shiftCoefficient + patientCoefficient)
                    });
                }
            }

            EmptyMessage = ScheduleRows.Any() ? string.Empty : "Chưa có lịch làm việc trong tuần này.";
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }

    public class PersonalScheduleRow
    {
        public DateTime ScheduleDate { get; set; }
        public string ShiftName { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal ShiftHours { get; set; }
        public decimal ShiftCoefficient { get; set; }
        public decimal PatientCoefficient { get; set; }
        public decimal ConvertedHours { get; set; }
    }
}
