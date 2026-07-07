using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;

namespace ClinicManagement.UI.ViewModels
{
    public class PersonalSalaryViewModel : ViewModelBase
    {
        private string _currentSalaryMessage;
        private string _salaryHistoryMessage;

        public int SelectedMonth { get; }
        public int SelectedYear { get; }
        public decimal DefaultShiftCoefficient { get; private set; }
        public decimal HourlyRate { get; private set; }
        public decimal ReceptionistCoefficient { get; private set; }
        public decimal TotalShiftAmount { get; private set; }
        public decimal TotalHours { get; private set; }
        public decimal TotalAmount { get; private set; }
        public decimal LifetimeFinalizedSalary { get; private set; }

        public ObservableCollection<PersonalSalaryShiftRow> CurrentShiftRows { get; }
        public ObservableCollection<PersonalSalaryHistoryRow> SalaryHistoryRows { get; }

        public string CurrentPeriodText => $"Tháng {SelectedMonth}/{SelectedYear}";

        public string CurrentSalaryMessage
        {
            get => _currentSalaryMessage;
            set => SetProperty(ref _currentSalaryMessage, value);
        }

        public string SalaryHistoryMessage
        {
            get => _salaryHistoryMessage;
            set => SetProperty(ref _salaryHistoryMessage, value);
        }

        public PersonalSalaryViewModel()
        {
            UserContext.CheckRole(EmployeeRole.Dentist, EmployeeRole.Receptionist);

            SelectedMonth = DateTime.Today.Month;
            SelectedYear = DateTime.Today.Year;
            CurrentShiftRows = new ObservableCollection<PersonalSalaryShiftRow>();
            SalaryHistoryRows = new ObservableCollection<PersonalSalaryHistoryRow>();

            LoadData();
        }

        private void LoadData()
        {
            var currentUser = UserContext.CurrentUser;
            var startDate = new DateTime(SelectedYear, SelectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            CurrentShiftRows.Clear();
            SalaryHistoryRows.Clear();

            using (var context = new ClinicDbContext())
            {
                var config = context.SalaryConfigurations.FirstOrDefault();
                DefaultShiftCoefficient = config?.DefaultShiftCoefficient ?? 1.0m;
                HourlyRate = config?.HourlyRate ?? 0m;
                ReceptionistCoefficient = config?.ReceptionistCoefficient ?? 1.0m;
                var degreeCoefficient = 1.0m;
                if (currentUser.Role == EmployeeRole.Dentist && currentUser.Degree.HasValue && config != null)
                {
                    degreeCoefficient = context.DentistDegreeSalaryCoefficients
                        .Where(c => c.SalaryConfigurationId == config.Id && c.Degree == currentUser.Degree.Value)
                        .Select(c => (decimal?)c.Coefficient)
                        .FirstOrDefault() ?? 1.0m;
                }
                var employeeCoefficient = currentUser.Role == EmployeeRole.Receptionist ? ReceptionistCoefficient : 1.0m;

                var schedules = context.EmployeeSchedules
                    .Include(s => s.WorkShift)
                    .Where(s => s.EmployeeId == currentUser.Id
                        && s.ScheduleDate >= startDate
                        && s.ScheduleDate <= endDate)
                    .OrderBy(s => s.ScheduleDate)
                    .ThenBy(s => s.WorkShift.StartTime)
                    .ToList();

                foreach (var schedule in schedules)
                {
                    var shift = schedule.WorkShift;
                    var shiftHours = shift?.TotalHours ?? 0m;
                    var shiftCoefficient = (decimal)(schedule.ShiftCoefficient ?? (float)DefaultShiftCoefficient);
                    var patientCoefficient = (decimal)schedule.PatientCoefficient;
                    var convertedHours = shiftHours * (shiftCoefficient + patientCoefficient);
                    var amount = convertedHours * degreeCoefficient * employeeCoefficient * HourlyRate;

                    CurrentShiftRows.Add(new PersonalSalaryShiftRow
                    {
                        ScheduleDate = schedule.ScheduleDate,
                        ShiftName = shift?.Name,
                        ShiftHours = shiftHours,
                        ShiftCoefficient = shiftCoefficient,
                        PatientCoefficient = patientCoefficient,
                        ConvertedHours = convertedHours,
                        HourlyRate = HourlyRate,
                        Amount = amount
                    });
                }

                TotalShiftAmount = CurrentShiftRows.Sum(r => r.Amount);
                TotalHours = CurrentShiftRows.Sum(r => r.ConvertedHours);

                TotalAmount = TotalShiftAmount;
                CurrentSalaryMessage = CurrentShiftRows.Any() ? string.Empty : "Chưa có dữ liệu lương trong kỳ này.";

                var history = context.SalaryRecords
                    .Where(r => r.EmployeeId == currentUser.Id && r.IsFinalized)
                    .OrderByDescending(r => r.Year)
                    .ThenByDescending(r => r.Month)
                    .ToList();

                foreach (var record in history)
                {
                    SalaryHistoryRows.Add(new PersonalSalaryHistoryRow
                    {
                        Month = record.Month,
                        Year = record.Year,
                        TotalHours = record.TotalHours,
                        TotalAmount = record.TotalAmount,
                        FinalizedDate = record.FinalizedDate
                    });
                }

                LifetimeFinalizedSalary = SalaryHistoryRows.Sum(r => r.TotalAmount);
                SalaryHistoryMessage = SalaryHistoryRows.Any() ? string.Empty : "Chưa có lịch sử lương.";
            }

            OnPropertyChanged(nameof(DefaultShiftCoefficient));
            OnPropertyChanged(nameof(HourlyRate));
            OnPropertyChanged(nameof(ReceptionistCoefficient));
            OnPropertyChanged(nameof(TotalShiftAmount));
            OnPropertyChanged(nameof(TotalHours));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(LifetimeFinalizedSalary));
        }
    }

    public class PersonalSalaryShiftRow
    {
        public DateTime ScheduleDate { get; set; }
        public string ShiftName { get; set; }
        public decimal ShiftHours { get; set; }
        public decimal ShiftCoefficient { get; set; }
        public decimal PatientCoefficient { get; set; }
        public decimal ConvertedHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Amount { get; set; }
    }

    public class PersonalSalaryHistoryRow
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? FinalizedDate { get; set; }
    }
}
