using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ClinicManagement.Core;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class WorkShiftServiceTests
    {
        private WorkShiftService _workShiftService;
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            _fakeUow = new FakeUnitOfWork();
            _workShiftService = new WorkShiftService(_fakeUow);
            
            // Seed base data
            _fakeUow.WorkShifts.Add(new WorkShift { Id = 1, Name = "Ca sáng", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0) });
            _fakeUow.WorkShifts.Add(new WorkShift { Id = 2, Name = "Ca chiều", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0) });
            
            // Seed employees
            _fakeUow.Employees.Add(new Employee { Id = 1, Role = EmployeeRole.Receptionist, ContractStatus = ContractStatus.Working });
            _fakeUow.Employees.Add(new Employee { Id = 2, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Working });
            _fakeUow.Employees.Add(new Employee { Id = 3, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Working });
            _fakeUow.Employees.Add(new Employee { Id = 4, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Working });
            _fakeUow.Employees.Add(new Employee { Id = 5, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Working });
            _fakeUow.Employees.Add(new Employee { Id = 6, Role = EmployeeRole.Dentist, ContractStatus = ContractStatus.Resigned });
        }

        [TestMethod]
        public void AddWorkShift_OverlapTime_ThrowsException()
        {
            // Input: Thêm ca mới (09:00 - 11:00) trùng vào Ca sáng (08:00 - 12:00)
            // Expected CHÍNH XÁC: Exception "Khung giờ trùng với ca Ca sáng."
            var newShift = new WorkShift { Name = "Ca trùng", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(11, 0, 0) };
            
            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.AddWorkShift(newShift));
            Assert.IsTrue(ex.Message.Contains("Khung giờ trùng"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_MissingReceptionist_ThrowsException()
        {
            // Input: Xếp lịch cho 4 nha sĩ, 0 lễ tân vào 1 ca
            // Expected CHÍNH XÁC: Exception "Mỗi ca làm việc phải có ít nhất 1 Lễ tân."
            var schedules = new List<EmployeeSchedule>
            {
                new EmployeeSchedule { EmployeeId = 2, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 3, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 4, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 5, WorkShiftId = 1, ScheduleDate = DateTime.Today }
            };

            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7)));
            Assert.IsTrue(ex.Message.Contains("phải có ít nhất 1 Lễ tân và 4 Nha sĩ"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_MissingDentist_ThrowsException()
        {
            // Input: Xếp lịch cho 1 lễ tân, 3 nha sĩ vào 1 ca
            // Expected CHÍNH XÁC: Exception yêu cầu đủ 1 lễ tân 4 nha sĩ
            var schedules = new List<EmployeeSchedule>
            {
                new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 2, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 3, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 4, WorkShiftId = 1, ScheduleDate = DateTime.Today }
            };

            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7)));
            Assert.IsTrue(ex.Message.Contains("phải có ít nhất 1 Lễ tân và 4 Nha sĩ"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_ResignedEmployee_ThrowsException()
        {
            // Input: Xếp lịch tương lai cho nhân viên đã thôi việc (Id = 6)
            // Expected CHÍNH XÁC: Exception chặn xếp lịch
            var schedules = new List<EmployeeSchedule>
            {
                new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = DateTime.Today.AddDays(1) },
                new EmployeeSchedule { EmployeeId = 2, WorkShiftId = 1, ScheduleDate = DateTime.Today.AddDays(1) },
                new EmployeeSchedule { EmployeeId = 3, WorkShiftId = 1, ScheduleDate = DateTime.Today.AddDays(1) },
                new EmployeeSchedule { EmployeeId = 4, WorkShiftId = 1, ScheduleDate = DateTime.Today.AddDays(1) },
                new EmployeeSchedule { EmployeeId = 6, WorkShiftId = 1, ScheduleDate = DateTime.Today.AddDays(1) } // Resigned
            };

            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7)));
            Assert.IsTrue(ex.Message.Contains("đã nghỉ việc, không được xếp lịch"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_ShiftCoefficientOutOfRange_ThrowsException()
        {
            var schedules = CreateValidSchedules();
            schedules[0].ShiftCoefficient = 1.6f;

            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7)));

            Assert.IsTrue(ex.Message.Contains("Hệ số ca"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_NegativePatientCoefficient_ThrowsException()
        {
            var schedules = CreateValidSchedules();
            schedules[0].PatientCoefficient = -0.1f;

            var ex = Assert.ThrowsException<Exception>(() => _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7)));

            Assert.IsTrue(ex.Message.Contains("Hệ số bệnh nhân"));
        }

        [TestMethod]
        public void SaveWeeklySchedules_ValidComposition_ShouldSaveSuccessfully()
        {
            // Input: Xếp đúng 1 Lễ tân + 4 Nha sĩ đang working
            // Expected CHÍNH XÁC: Lưu thành công, không throw exception
            var schedules = new List<EmployeeSchedule>
            {
                new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 2, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 3, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 4, WorkShiftId = 1, ScheduleDate = DateTime.Today },
                new EmployeeSchedule { EmployeeId = 5, WorkShiftId = 1, ScheduleDate = DateTime.Today }
            };

            // Should not throw
            _workShiftService.SaveWeeklySchedules(schedules, DateTime.Today, DateTime.Today.AddDays(7));
            
            var saved = _workShiftService.GetSchedulesByDateRange(DateTime.Today, DateTime.Today);
            Assert.AreEqual(5, saved.Count);
        }

        private List<EmployeeSchedule> CreateValidSchedules()
        {
            return new List<EmployeeSchedule>
            {
                new EmployeeSchedule { EmployeeId = 1, WorkShiftId = 1, ScheduleDate = DateTime.Today, ShiftCoefficient = 1.0f },
                new EmployeeSchedule { EmployeeId = 2, WorkShiftId = 1, ScheduleDate = DateTime.Today, ShiftCoefficient = 1.0f },
                new EmployeeSchedule { EmployeeId = 3, WorkShiftId = 1, ScheduleDate = DateTime.Today, ShiftCoefficient = 1.0f },
                new EmployeeSchedule { EmployeeId = 4, WorkShiftId = 1, ScheduleDate = DateTime.Today, ShiftCoefficient = 1.0f },
                new EmployeeSchedule { EmployeeId = 5, WorkShiftId = 1, ScheduleDate = DateTime.Today, ShiftCoefficient = 1.0f }
            };
        }
    }
}
