using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SalaryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public List<SalaryConfiguration> GetAllConfigurations()
        {
            return _unitOfWork.SalaryConfigurations.GetAll().ToList();
        }

        public void UpdateConfiguration(SalaryConfiguration config)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            ValidateConfiguration(config);

            var existing = _unitOfWork.SalaryConfigurations.GetById(config.Id);
            if (existing != null)
            {
                existing.HourlyRate = config.HourlyRate;
                existing.DefaultShiftCoefficient = config.DefaultShiftCoefficient;
                existing.ReceptionistCoefficient = config.ReceptionistCoefficient;

                SyncDentistDegreeCoefficients(existing.Id, config.DentistDegreeCoefficients);
                _unitOfWork.Save();
            }
            else
            {
                _unitOfWork.SalaryConfigurations.Add(config);
                _unitOfWork.Save();
            }
        }

        public List<SalaryRecord> CalculatePayroll(int month, int year)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            // Nếu đã finalized thì lấy từ DB ra
            var existingRecords = _unitOfWork.SalaryRecords.Find(r => r.Month == month && r.Year == year).ToList();
            if (existingRecords.Any(r => r.IsFinalized))
            {
                throw new Exception("Bảng lương tháng này đã được chốt và khóa. Không thể tính toán lại.");
            }

            // Xóa bản draft cũ nếu có
            foreach (var draft in existingRecords)
            {
                _unitOfWork.SalaryRecords.Remove(draft);
            }
            _unitOfWork.Save(); // Save để xóa bản nháp

            var config = GetEffectiveConfiguration();
            var employees = _unitOfWork.Employees.GetAll()
                .Where(e => e.ContractStatus == ContractStatus.Working
                    && (e.Role == EmployeeRole.Dentist || e.Role == EmployeeRole.Receptionist))
                .ToList();
            var newRecords = new List<SalaryRecord>();

            // Tính số giờ làm việc
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var schedules = _unitOfWork.EmployeeSchedules.Find(s => s.ScheduleDate >= startDate && s.ScheduleDate <= endDate).ToList();
            var workShifts = _unitOfWork.WorkShifts.GetAll().ToDictionary(w => w.Id);

            foreach (var emp in employees)
            {
                var empSchedules = schedules.Where(s => s.EmployeeId == emp.Id).ToList();
                decimal totalHours = 0;
                decimal totalAmount = 0;
                foreach (var sch in empSchedules)
                {
                    if (workShifts.TryGetValue(sch.WorkShiftId, out var shift))
                    {
                        var duration = shift.EndTime - shift.StartTime;
                        var shiftHours = (decimal)duration.TotalHours;
                        var convertedHours = shiftHours * ((decimal)(sch.ShiftCoefficient ?? (float)config.DefaultShiftCoefficient) + (decimal)sch.PatientCoefficient);
                        totalHours += convertedHours;
                        totalAmount += convertedHours * GetDegreeCoefficient(emp, config) * GetEmployeeCoefficient(emp, config) * config.HourlyRate;
                    }
                }

                var record = new SalaryRecord
                {
                    EmployeeId = emp.Id,
                    Month = month,
                    Year = year,
                    TotalHours = totalHours,
                    TotalAmount = totalAmount,
                    IsFinalized = false,
                    Employee = emp // for UI display
                };
                newRecords.Add(record);
                _unitOfWork.SalaryRecords.Add(record);
            }

            _unitOfWork.Save();
            return newRecords;
        }

        public void FinalizePayroll(int month, int year)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var payrollMonth = new DateTime(year, month, 1);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (payrollMonth >= currentMonth)
                throw new Exception("Chỉ được chốt lương cho kỳ đã kết thúc.");

            var records = _unitOfWork.SalaryRecords.Find(r => r.Month == month && r.Year == year).ToList();
            if (records.Count == 0)
                throw new Exception("Chưa có bảng lương để chốt. Hãy tính lương trước.");

            if (records.Any(r => r.IsFinalized))
                throw new Exception("Bảng lương tháng này đã được chốt và khóa.");

            var config = GetEffectiveConfiguration();

            foreach (var record in records)
            {
                var employee = _unitOfWork.Employees.GetById(record.EmployeeId);
                record.IsFinalized = true;
                record.FinalizedDate = DateTime.Now;

                var snapshot = new SalaryFormulaSnapshot
                {
                    Id = record.Id, // Khóa chính 1-1 phải bằng với khóa của Record
                    SalaryRecordId = record.Id,
                    Role = employee.Role,
                    BaseSalary = config.DefaultShiftCoefficient,
                    HourlyRate = config.HourlyRate,
                    Allowance = employee.Role == EmployeeRole.Receptionist
                        ? config.ReceptionistCoefficient
                        : GetDegreeCoefficient(employee, config),
                    SnapshotDate = DateTime.Now
                };

                _unitOfWork.SalaryFormulaSnapshots.Add(snapshot);
                record.FormulaSnapshot = snapshot;
            }

            _unitOfWork.Save();
        }

        public List<SalaryRecord> GetSalaryRecords(int month, int year)
        {
            return _unitOfWork.SalaryRecords.AsQueryable()
                .Include(r => r.Employee)
                .Where(r => r.Month == month && r.Year == year)
                .OrderBy(r => r.EmployeeId)
                .ToList();
        }

        private SalaryConfiguration GetEffectiveConfiguration()
        {
            var config = _unitOfWork.SalaryConfigurations.GetAll().FirstOrDefault();
            return config ?? CreateDefaultConfiguration();
        }

        private decimal GetDegreeCoefficient(Employee employee, SalaryConfiguration config)
        {
            if (employee.Role != EmployeeRole.Dentist)
                return 1.0m;

            if (!employee.Degree.HasValue)
                return 1.0m;

            var coefficient = config.DentistDegreeCoefficients?.FirstOrDefault(c => c.Degree == employee.Degree.Value)
                ?? _unitOfWork.DentistDegreeSalaryCoefficients
                    .Find(c => c.SalaryConfigurationId == config.Id && c.Degree == employee.Degree.Value)
                    .FirstOrDefault();

            return coefficient?.Coefficient ?? 1.0m;
        }

        private decimal GetEmployeeCoefficient(Employee employee, SalaryConfiguration config)
        {
            return employee.Role == EmployeeRole.Receptionist ? config.ReceptionistCoefficient : 1.0m;
        }

        private SalaryConfiguration CreateDefaultConfiguration()
        {
            var config = new SalaryConfiguration
            {
                HourlyRate = 1000m,
                DefaultShiftCoefficient = 1.0m,
                ReceptionistCoefficient = 1.0m
            };

            foreach (var degree in DentistDegrees())
            {
                config.DentistDegreeCoefficients.Add(new DentistDegreeSalaryCoefficient
                {
                    Degree = degree,
                    Coefficient = 1.0m
                });
            }

            return config;
        }

        private void ValidateConfiguration(SalaryConfiguration config)
        {
            if (config.HourlyRate < 1000m)
                throw new ArgumentException("Số tiền một giờ phải tối thiểu 1.000 VND.");

            if (config.DefaultShiftCoefficient < 1.0m || config.DefaultShiftCoefficient > 1.5m)
                throw new ArgumentException("Hệ số ca làm việc mặc định phải từ 1.0 đến 1.5.");

            if (config.ReceptionistCoefficient <= 0)
                throw new ArgumentException("Hệ số nhân viên Lễ tân phải là số dương.");

            var dentistCoefficients = config.DentistDegreeCoefficients?.ToList() ?? new List<DentistDegreeSalaryCoefficient>();
            foreach (var degree in DentistDegrees())
            {
                var coefficient = dentistCoefficients.FirstOrDefault(c => c.Degree == degree);
                if (coefficient == null)
                    throw new ArgumentException("Thiếu hệ số trình độ cho Nha sĩ.");

                if (coefficient.Coefficient <= 0)
                    throw new ArgumentException("Hệ số trình độ Nha sĩ phải là số dương.");
            }
        }

        private void SyncDentistDegreeCoefficients(int salaryConfigurationId, IEnumerable<DentistDegreeSalaryCoefficient> coefficients)
        {
            var existingCoefficients = _unitOfWork.DentistDegreeSalaryCoefficients
                .Find(c => c.SalaryConfigurationId == salaryConfigurationId)
                .ToList();

            foreach (var degree in DentistDegrees())
            {
                var source = coefficients.First(c => c.Degree == degree);
                var existing = existingCoefficients.FirstOrDefault(c => c.Degree == degree);

                if (existing == null)
                {
                    _unitOfWork.DentistDegreeSalaryCoefficients.Add(new DentistDegreeSalaryCoefficient
                    {
                        SalaryConfigurationId = salaryConfigurationId,
                        Degree = degree,
                        Coefficient = source.Coefficient
                    });
                }
                else
                {
                    existing.Coefficient = source.Coefficient;
                }
            }
        }

        private static IEnumerable<AcademicDegree> DentistDegrees()
        {
            return new[]
            {
                AcademicDegree.Doctor,
                AcademicDegree.Master,
                AcademicDegree.PhD,
                AcademicDegree.AssociateProfessor,
                AcademicDegree.Professor
            };
        }
    }
}
