using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ClinicManagement.Core;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class SalaryServiceTests
    {
        private SalaryService _salaryService;
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            _fakeUow = new FakeUnitOfWork();
            _salaryService = new SalaryService(_fakeUow);
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Manager };

            _fakeUow.SalaryConfigurations.Add(new SalaryConfiguration { Id = 1, HourlyRate = 500, DefaultShiftCoefficient = 1.0m, ReceptionistCoefficient = 1.0m });
            _fakeUow.Employees.Add(new Employee { Id = 1, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Working });
            _fakeUow.WorkShifts.Add(new WorkShift { Id = 1, Name = "Ca sáng", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0) });
            _fakeUow.EmployeeSchedules.Add(new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = new DateTime(2026, 5, 1), ShiftCoefficient = 1.0f, PatientCoefficient = 0f });
            _fakeUow.EmployeeSchedules.Add(new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = new DateTime(2026, 6, 1), ShiftCoefficient = 1.0f, PatientCoefficient = 0f });
        }

        [TestMethod]
        public void CalculatePayroll_DraftsCanBeRecalculated()
        {
            // Input: Tính lương lần 1, sau đó sửa config và tính lương lần 2 (chưa chốt)
            // Expected CHÍNH XÁC: Bản draft cũ bị xóa, bản draft mới ăn theo config mới
            _salaryService.CalculatePayroll(5, 2026);
            var records1 = _salaryService.GetSalaryRecords(5, 2026);
            Assert.AreEqual(1, records1.Count);
            Assert.AreEqual(2000, records1[0].TotalAmount);

            // Đổi config
            _fakeUow.SalaryConfigurations.GetById(1).HourlyRate = 600;
            
            // Tính lại
            _salaryService.CalculatePayroll(5, 2026);
            var records2 = _salaryService.GetSalaryRecords(5, 2026);
            
            Assert.AreEqual(1, records2.Count); // Vẫn chỉ có 1 bản ghi vì bản nháp cũ bị xóa
            Assert.AreEqual(2400, records2[0].TotalAmount);
            Assert.IsFalse(records2[0].IsFinalized);
        }

        [TestMethod]
        public void FinalizePayroll_LocksSalaryRecord()
        {
            // Input: Gọi FinalizePayroll cho tháng 5
            // Expected CHÍNH XÁC: Tất cả Record của tháng 5 IsFinalized = true, gọi Calculate lại không thay đổi gì
            _salaryService.CalculatePayroll(5, 2026);
            _salaryService.FinalizePayroll(5, 2026);

            var records = _salaryService.GetSalaryRecords(5, 2026);
            Assert.IsTrue(records.All(r => r.IsFinalized));
        }

        [TestMethod]
        public void FinalizePayroll_CreatesFormulaSnapshot()
        {
            // Input: Chốt lương tháng 5
            // Expected CHÍNH XÁC: Có 1 bản ghi SalaryFormulaSnapshot được tạo ra tương ứng
            _salaryService.CalculatePayroll(5, 2026);
            _salaryService.FinalizePayroll(5, 2026);

            var record = _salaryService.GetSalaryRecords(5, 2026).First();
            var snapshots = _fakeUow.SalaryFormulaSnapshots.GetAll().ToList();

            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual(record.Id, snapshots[0].SalaryRecordId);
            Assert.AreEqual(500, snapshots[0].HourlyRate);
            Assert.AreEqual(1.0m, snapshots[0].BaseSalary);
            Assert.AreEqual(1.0m, snapshots[0].Allowance);
        }

        [TestMethod]
        public void FinalizePayroll_CurrentOrFuturePeriod_ShouldThrowException()
        {
            var current = DateTime.Today;
            var future = current.AddMonths(1);

            _fakeUow.SalaryRecords.Add(new SalaryRecord { EmployeeId = 1, Month = current.Month, Year = current.Year });
            _fakeUow.SalaryRecords.Add(new SalaryRecord { EmployeeId = 1, Month = future.Month, Year = future.Year });

            var currentEx = Assert.ThrowsException<Exception>(() => _salaryService.FinalizePayroll(current.Month, current.Year));
            var futureEx = Assert.ThrowsException<Exception>(() => _salaryService.FinalizePayroll(future.Month, future.Year));

            Assert.IsTrue(currentEx.Message.Contains("kỳ đã kết thúc"));
            Assert.IsTrue(futureEx.Message.Contains("kỳ đã kết thúc"));
        }

        [TestMethod]
        public void CalculatePayroll_AfterFinalized_ShouldThrowException()
        {
            // Input: Tính lương, chốt lương, sau đó gọi lại CalculatePayroll
            // Expected CHÍNH XÁC: Ném ra Exception "Bảng lương tháng này đã được chốt và khóa..."
            _salaryService.CalculatePayroll(6, 2026);
            _salaryService.FinalizePayroll(6, 2026);

            var ex = Assert.ThrowsException<Exception>(() => _salaryService.CalculatePayroll(6, 2026));
            Assert.IsTrue(ex.Message.Contains("đã được chốt và khóa"));
        }

        [TestMethod]
        public void CalculatePayroll_UsesSeparatedDentistDegreeAndReceptionistCoefficients()
        {
            var config = _fakeUow.SalaryConfigurations.GetById(1);
            config.ReceptionistCoefficient = 1.5m;
            config.DefaultShiftCoefficient = 1.0m;
            config.DentistDegreeCoefficients.Add(new DentistDegreeSalaryCoefficient
            {
                SalaryConfigurationId = 1,
                Degree = AcademicDegree.Master,
                Coefficient = 2.0m
            });

            _fakeUow.Employees.Add(new Employee
            {
                Id = 2,
                Role = EmployeeRole.Receptionist,
                ContractStatus = ContractStatus.Working
            });
            _fakeUow.Employees.Add(new Employee
            {
                Id = 3,
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                ContractStatus = ContractStatus.Working
            });
            _fakeUow.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 2,
                WorkShiftId = 1,
                ScheduleDate = new DateTime(2026, 5, 2),
                ShiftCoefficient = 1.25f,
                PatientCoefficient = 0.25f
            });
            _fakeUow.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 3,
                WorkShiftId = 1,
                ScheduleDate = new DateTime(2026, 5, 2),
                ShiftCoefficient = 1.25f,
                PatientCoefficient = 0.25f
            });

            var records = _salaryService.CalculatePayroll(5, 2026);

            Assert.AreEqual(4500, records.Single(r => r.EmployeeId == 2).TotalAmount);
            Assert.AreEqual(6000, records.Single(r => r.EmployeeId == 3).TotalAmount);
        }
    }
}
