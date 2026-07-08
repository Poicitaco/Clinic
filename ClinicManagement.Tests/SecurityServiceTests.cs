using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ClinicManagement.Core;
using ClinicManagement.Business;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class SecurityServiceTests
    {
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            _fakeUow = new FakeUnitOfWork();
        }

        [TestMethod]
        public void EmployeeService_AddEmployee_RoleDentist_ThrowsUnauthorized()
        {
            // Input: UserContext là Dentist, gọi EmployeeService.CreateEmployee
            // Expected CHÍNH XÁC: Ném ngoại lệ UnauthorizedAccessException "Bạn không có quyền thực hiện thao tác này."
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Dentist };
            var service = new EmployeeService(_fakeUow);
            
            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.CreateEmployee(new Employee()));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void SalaryService_FinalizePayroll_RoleReceptionist_ThrowsUnauthorized()
        {
            // Input: UserContext là Lễ tân, gọi SalaryService.FinalizePayroll
            // Expected CHÍNH XÁC: Ném ngoại lệ UnauthorizedAccessException "Bạn không có quyền thực hiện thao tác này."
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Receptionist };
            var service = new SalaryService(_fakeUow);
            
            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.FinalizePayroll(5, 2026));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void SalaryService_GetAllConfigurations_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Receptionist };
            var service = new SalaryService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetAllConfigurations());
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void SalaryService_GetSalaryRecords_RoleDentist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Dentist };
            var service = new SalaryService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetSalaryRecords(5, 2026));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void StatisticService_GetRevenue_RoleDentist_ThrowsUnauthorized()
        {
            // Input: UserContext là Dentist, gọi StatisticService.GetTotalRevenue
            // Expected CHÍNH XÁC: Ném ngoại lệ UnauthorizedAccessException "Bạn không có quyền thực hiện thao tác này."
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Dentist };
            var service = new StatisticService(_fakeUow);
            
            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetTotalRevenue(DateTime.Today, DateTime.Today));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }
        
        [TestMethod]
        public void EmployeeService_GetProfile_OnlyShowsOwnData()
        {
            // Input: UserContext là Dentist (Id=1), cố gắng xem Profile của Receptionist (Id=2)
            // Expected CHÍNH XÁC: Ném ngoại lệ UnauthorizedAccessException "Chỉ có thể xem hồ sơ của chính mình." (ngoại trừ Manager)
            _fakeUow.Employees.Add(new Employee { Id = 1, Role = EmployeeRole.Dentist });
            _fakeUow.Employees.Add(new Employee { Id = 2, Role = EmployeeRole.Receptionist });
            
            UserContext.CurrentUser = _fakeUow.Employees.GetById(1);
            var service = new EmployeeService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetEmployeeById(2));
            Assert.IsTrue(ex.Message.Contains("Chỉ có thể xem hồ sơ của chính mình"));
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_RoleDentist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Dentist };
            var service = new AppointmentService(_fakeUow);

            var appointment = new Appointment
            {
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(1).AddHours(1),
                Status = AppointmentStatus.Pending
            };

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.AddAppointment(appointment));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_RoleReceptionist_ShouldCreateSuccessfully()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(1).AddHours(9);
            SeedDentistWorkShift(1, start.Date);

            var appointment = new Appointment
            {
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending
            };

            service.AddAppointment(appointment);

            Assert.AreEqual(1, _fakeUow.Appointments.GetAll().Count());
            Assert.AreEqual(2, appointment.CreatedById);
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_OverlappingSameDentist_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(1).AddHours(9);
            SeedDentistWorkShift(1, start.Date);

            _fakeUow.Appointments.Add(new Appointment
            {
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending
            });

            var overlap = new Appointment
            {
                PatientName = "Tran Thi B",
                PhoneNumber = "0987654321",
                DentistId = 1,
                StartTime = start.AddMinutes(30),
                EndTime = start.AddHours(2),
                Status = AppointmentStatus.Pending
            };

            var ex = Assert.ThrowsException<Exception>(() => service.AddAppointment(overlap));
            Assert.IsTrue(ex.Message.Contains("Nha sĩ đã có lịch hẹn"));
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_OverlappingDifferentDentist_ShouldCreateSuccessfully()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(1).AddHours(9);
            SeedDentistWorkShift(2, start.Date);

            _fakeUow.Appointments.Add(new Appointment
            {
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending
            });

            service.AddAppointment(new Appointment
            {
                PatientName = "Tran Thi B",
                PhoneNumber = "0987654321",
                DentistId = 2,
                StartTime = start.AddMinutes(30),
                EndTime = start.AddHours(2),
                Status = AppointmentStatus.Pending
            });

            Assert.AreEqual(2, _fakeUow.Appointments.GetAll().Count());
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_InvalidPatientName_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(1).AddHours(9);
            SeedDentistWorkShift(1, start.Date);

            var appointment = CreateValidAppointment(start);
            appointment.PatientName = "A1";

            var ex = Assert.ThrowsException<Exception>(() => service.AddAppointment(appointment));
            Assert.IsTrue(ex.Message.Contains("Tên bệnh nhân"));
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_InvalidPhone_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(1).AddHours(9);
            SeedDentistWorkShift(1, start.Date);

            var appointment = CreateValidAppointment(start);
            appointment.PhoneNumber = "123";

            var ex = Assert.ThrowsException<Exception>(() => service.AddAppointment(appointment));
            Assert.IsTrue(ex.Message.Contains("Số điện thoại"));
        }

        [TestMethod]
        public void AppointmentService_AddAppointment_DentistOutsideWorkShift_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var date = DateTime.Today.AddDays(1);
            SeedDentistWorkShift(1, date);

            var appointment = CreateValidAppointment(date.AddHours(18));

            var ex = Assert.ThrowsException<Exception>(() => service.AddAppointment(appointment));
            Assert.IsTrue(ex.Message.Contains("không có ca làm việc"));
        }

        [TestMethod]
        public void AppointmentService_UpdateAppointment_DentistOutsideWorkShift_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var date = DateTime.Today.AddDays(1);
            SeedDentistWorkShift(1, date);
            _fakeUow.Appointments.Add(CreateValidAppointment(date.AddHours(9)));

            var appointment = CreateValidAppointment(date.AddHours(18));
            appointment.Id = 1;

            var ex = Assert.ThrowsException<Exception>(() => service.UpdateAppointment(appointment));
            Assert.IsTrue(ex.Message.Contains("không có ca làm việc"));
        }

        [TestMethod]
        public void AppointmentService_UpdateAppointment_SavesNotes()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var date = DateTime.Today.AddDays(1);
            SeedDentistWorkShift(1, date);
            _fakeUow.Appointments.Add(CreateValidAppointment(date.AddHours(9)));

            var appointment = CreateValidAppointment(date.AddHours(10));
            appointment.Id = 1;
            appointment.Notes = "Benh nhan can tu van ky truoc khi dieu tri.";

            service.UpdateAppointment(appointment);

            Assert.AreEqual("Benh nhan can tu van ky truoc khi dieu tri.", _fakeUow.Appointments.GetById(1).Notes);
        }

        [TestMethod]
        public void AppointmentService_UpdateAppointment_UnchangedScheduleWithoutWorkShift_SavesNotes()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var start = DateTime.Today.AddDays(3).AddHours(13);
            _fakeUow.Employees.Add(new Employee
            {
                Id = 1,
                Role = EmployeeRole.Dentist,
                ContractStatus = ContractStatus.Working
            });
            _fakeUow.Appointments.Add(new Appointment
            {
                Id = 1,
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending
            });

            service.UpdateAppointment(new Appointment
            {
                Id = 1,
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending,
                Notes = "Cap nhat ghi chu khong doi lich"
            });

            Assert.AreEqual("Cap nhat ghi chu khong doi lich", _fakeUow.Appointments.GetById(1).Notes);
        }

        [TestMethod]
        public void AppointmentService_UpdatePastAppointment_ShouldSaveNotesAndStatus()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var pastStart = DateTime.Today.AddDays(-2).AddHours(9);
            _fakeUow.Employees.Add(new Employee
            {
                Id = 1,
                Role = EmployeeRole.Dentist,
                ContractStatus = ContractStatus.Working
            });
            _fakeUow.Appointments.Add(new Appointment
            {
                Id = 1,
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = pastStart,
                EndTime = pastStart.AddHours(1),
                Status = AppointmentStatus.Pending
            });

            service.UpdateAppointment(new Appointment
            {
                Id = 1,
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = pastStart.AddMinutes(10),
                EndTime = pastStart.AddMinutes(70),
                Status = AppointmentStatus.Completed,
                Notes = "Cap nhat lich hen cu"
            });

            var updated = _fakeUow.Appointments.GetById(1);
            Assert.AreEqual(AppointmentStatus.Completed, updated.Status);
            Assert.AreEqual("Cap nhat lich hen cu", updated.Notes);
            Assert.AreEqual(pastStart.AddMinutes(10), updated.StartTime);
        }

        [TestMethod]
        public void AppointmentService_CancelAppointment_Completed_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            _fakeUow.Appointments.Add(new Appointment
            {
                Id = 1,
                Status = AppointmentStatus.Completed,
                StartTime = DateTime.Now.AddHours(2)
            });

            var ex = Assert.ThrowsException<Exception>(() => service.CancelAppointment(1));
            Assert.IsTrue(ex.Message.Contains("Chỉ lịch hẹn chưa diễn ra"));
        }

        [TestMethod]
        public void AppointmentService_CancelAppointment_AlreadyCancelled_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            _fakeUow.Appointments.Add(new Appointment
            {
                Id = 1,
                Status = AppointmentStatus.Cancelled,
                StartTime = DateTime.Now.AddHours(2)
            });

            var ex = Assert.ThrowsException<Exception>(() => service.CancelAppointment(1));
            Assert.IsTrue(ex.Message.Contains("Chỉ lịch hẹn chưa diễn ra"));
        }

        [TestMethod]
        public void AppointmentService_CancelAppointment_LessThanOneHour_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            _fakeUow.Appointments.Add(new Appointment
            {
                Id = 1,
                Status = AppointmentStatus.Pending,
                StartTime = DateTime.Now.AddMinutes(30)
            });

            var ex = Assert.ThrowsException<Exception>(() => service.CancelAppointment(1));
            Assert.IsTrue(ex.Message.Contains("ít nhất 1 tiếng"));
        }

        [TestMethod]
        public void AppointmentService_CancelAppointment_PendingMoreThanOneHour_ShouldCancel()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new AppointmentService(_fakeUow);
            var appointment = new Appointment
            {
                Id = 1,
                Status = AppointmentStatus.Pending,
                StartTime = DateTime.Now.AddHours(2)
            };
            _fakeUow.Appointments.Add(appointment);

            service.CancelAppointment(1);

            Assert.AreEqual(AppointmentStatus.Cancelled, appointment.Status);
        }

        [TestMethod]
        public void InvoiceService_AddInvoice_DuplicateExamination_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice
            {
                Id = 1,
                ExaminationId = 5,
                Status = InvoiceStatus.Pending
            });

            var invoice = new Invoice { PatientId = 1, ExaminationId = 5 };
            var details = new List<InvoiceDetail>
            {
                new InvoiceDetail { ServiceId = 1, Quantity = 1, UnitPrice = 100000 }
            };
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Khám", Price = 100000, IsActive = true });

            var ex = Assert.ThrowsException<Exception>(() => service.AddInvoice(invoice, details));
            Assert.IsTrue(ex.Message.Contains("đã có hóa đơn"));
        }

        [TestMethod]
        public void InvoiceService_AddInvoice_SetsCreatedByStatusDateAndTotal()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.PatientExaminations.Add(new PatientExamination
            {
                Id = 5,
                PatientId = 7,
                Status = ExaminationStatus.Finalized
            });
            var invoice = new Invoice
            {
                ExaminationId = 5,
                CreatedById = 99,
                CreatedDate = DateTime.MinValue,
                Status = InvoiceStatus.Paid,
                TotalAmount = 999999
            };
            var details = new List<InvoiceDetail>
            {
                new InvoiceDetail { ServiceId = 1, Quantity = 2, UnitPrice = 150000 },
                new InvoiceDetail { ServiceId = 2, Quantity = 1, UnitPrice = 50000 }
            };
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Trám răng", Price = 150000, IsActive = true });
            _fakeUow.Services.Add(new Service { Id = 2, Name = "Khám", Price = 50000, IsActive = true });
            var before = DateTime.Now.AddSeconds(-1);

            service.AddInvoice(invoice, details);

            Assert.AreEqual(2, invoice.CreatedById);
            Assert.AreEqual(7, invoice.PatientId);
            Assert.AreEqual(InvoiceStatus.Pending, invoice.Status);
            Assert.IsTrue(invoice.CreatedDate >= before);
            Assert.AreEqual(350000, invoice.TotalAmount);
            Assert.IsTrue(details.All(d => d.InvoiceId == invoice.Id));
            Assert.AreEqual(300000, details[0].Amount);
            Assert.AreEqual(50000, details[1].Amount);
        }

        [TestMethod]
        public void InvoiceService_AddInvoice_ReceptionistPreferentialPrice_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Khám", Price = 100000, IsActive = true });
            _fakeUow.PatientExaminations.Add(new PatientExamination { Id = 5, PatientId = 7, Status = ExaminationStatus.Finalized });

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.AddInvoice(
                new Invoice { ExaminationId = 5 },
                new List<InvoiceDetail> { new InvoiceDetail { ServiceId = 1, Quantity = 1, UnitPrice = 80000 } }));

            Assert.IsTrue(ex.Message.Contains("giá ưu đãi"));
        }

        [TestMethod]
        public void InvoiceService_AddInvoice_ManagerPreferentialPrice_ShouldCreateSuccessfully()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Khám", Price = 100000, IsActive = true });
            _fakeUow.PatientExaminations.Add(new PatientExamination { Id = 5, PatientId = 7, Status = ExaminationStatus.Finalized });
            var detail = new InvoiceDetail { ServiceId = 1, Quantity = 1, UnitPrice = 80000 };
            var invoice = new Invoice { ExaminationId = 5 };

            service.AddInvoice(invoice, new List<InvoiceDetail> { detail });

            Assert.AreEqual(80000, invoice.TotalAmount);
            Assert.AreEqual(80000, detail.Amount);
        }

        [TestMethod]
        public void InvoiceService_AddInvoice_InactiveService_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Khám", Price = 100000, IsActive = false });
            _fakeUow.PatientExaminations.Add(new PatientExamination { Id = 5, PatientId = 7, Status = ExaminationStatus.Finalized });

            var ex = Assert.ThrowsException<Exception>(() => service.AddInvoice(
                new Invoice { ExaminationId = 5 },
                new List<InvoiceDetail> { new InvoiceDetail { ServiceId = 1, Quantity = 1, UnitPrice = 100000 } }));

            Assert.IsTrue(ex.Message.Contains("tạm dừng"));
        }

        [TestMethod]
        public void PatientRecordService_AddRecord_Dentist_ForcesCurrentDentistAndToday()
        {
            UserContext.CurrentUser = new Employee { Id = 3, Role = EmployeeRole.Dentist };
            var service = new PatientRecordService(_fakeUow);
            var record = new PatientExamination
            {
                PatientId = 1,
                DentistId = 99,
                ExaminationDate = DateTime.Today.AddDays(-5),
                Diagnosis = "Sâu răng",
                TreatmentPlan = "Trám",
                Prescription = "Thuốc A",
                ProposedServices = "Trám răng",
                ReExamDate = DateTime.Today.AddDays(7)
            };

            service.AddRecord(record);

            Assert.AreEqual(3, record.DentistId);
            Assert.AreEqual(DateTime.Today, record.ExaminationDate);
            Assert.AreEqual("Thuốc A", record.Prescription);
            Assert.AreEqual("Trám răng", record.ProposedServices);
            Assert.AreEqual(DateTime.Today.AddDays(7), record.ReExamDate);
        }

        [TestMethod]
        public void PatientRecordService_UpdateRecord_DoesNotChangeImmutableDentistOrDate()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new PatientRecordService(_fakeUow);
            var originalDate = DateTime.Today.AddDays(-2);
            _fakeUow.PatientExaminations.Add(new PatientExamination
            {
                Id = 10,
                PatientId = 1,
                DentistId = 2,
                ExaminationDate = originalDate,
                Diagnosis = "Cũ",
                Status = ExaminationStatus.Draft
            });

            service.UpdateRecord(new PatientExamination
            {
                Id = 10,
                PatientId = 99,
                DentistId = 99,
                ExaminationDate = DateTime.Today,
                Diagnosis = "Mới",
                TreatmentPlan = "Điều trị",
                Status = ExaminationStatus.Finalized
            });

            var record = _fakeUow.PatientExaminations.GetById(10);
            Assert.AreEqual(1, record.PatientId);
            Assert.AreEqual(2, record.DentistId);
            Assert.AreEqual(originalDate, record.ExaminationDate);
            Assert.AreEqual("Mới", record.Diagnosis);
            Assert.AreEqual(ExaminationStatus.Finalized, record.Status);
        }

        [TestMethod]
        public void PatientRecordService_UpdateFinalized_Dentist_Throws()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Dentist };
            var service = new PatientRecordService(_fakeUow);
            _fakeUow.PatientExaminations.Add(new PatientExamination
            {
                Id = 11,
                PatientId = 1,
                DentistId = 2,
                Diagnosis = "Cũ",
                Status = ExaminationStatus.Finalized
            });

            var ex = Assert.ThrowsException<Exception>(() => service.UpdateRecord(new PatientExamination
            {
                Id = 11,
                Diagnosis = "Mới"
            }));
            Assert.IsTrue(ex.Message.Contains("Không thể sửa"));
        }

        [TestMethod]
        public void PatientRecordService_UpdateFinalized_ManagerRequiresReason()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new PatientRecordService(_fakeUow);
            _fakeUow.PatientExaminations.Add(new PatientExamination
            {
                Id = 12,
                PatientId = 1,
                DentistId = 2,
                Diagnosis = "Cũ",
                Status = ExaminationStatus.Finalized
            });

            var ex = Assert.ThrowsException<Exception>(() => service.UpdateRecord(new PatientExamination
            {
                Id = 12,
                Diagnosis = "Mới"
            }));
            Assert.IsTrue(ex.Message.Contains("lý do"));
        }

        [TestMethod]
        public void PatientRecordService_UpdateFinalized_ManagerWithReason_UpdatesButStaysFinalized()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new PatientRecordService(_fakeUow);
            _fakeUow.PatientExaminations.Add(new PatientExamination
            {
                Id = 13,
                PatientId = 1,
                DentistId = 2,
                Diagnosis = "Cũ",
                Status = ExaminationStatus.Finalized
            });

            service.UpdateRecord(new PatientExamination
            {
                Id = 13,
                Diagnosis = "Mới",
                TreatmentPlan = "Kế hoạch mới",
                Prescription = "Thuốc mới",
                ProposedServices = "Dịch vụ mới",
                ReExamDate = DateTime.Today.AddDays(14),
                ManagerInterventionReason = "Sửa theo yêu cầu chuyên môn",
                Status = ExaminationStatus.Draft
            });

            var record = _fakeUow.PatientExaminations.GetById(13);
            Assert.AreEqual("Mới", record.Diagnosis);
            Assert.AreEqual("Thuốc mới", record.Prescription);
            Assert.AreEqual("Dịch vụ mới", record.ProposedServices);
            Assert.AreEqual(DateTime.Today.AddDays(14), record.ReExamDate);
            Assert.AreEqual("Sửa theo yêu cầu chuyên môn", record.ManagerInterventionReason);
            Assert.AreEqual(ExaminationStatus.Finalized, record.Status);
        }

        [TestMethod]
        public void PatientRecordService_AddPatient_Receptionist_ShouldCreateSuccessfully()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new PatientRecordService(_fakeUow);

            service.AddPatient(new Patient
            {
                FullName = "Nguyen Van Test",
                PhoneNumber = "0901234567",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male
            });

            Assert.AreEqual(1, _fakeUow.Patients.GetAll().Count());
        }

        [TestMethod]
        public void PatientRecordService_UpdatePatient_Dentist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 3, Role = EmployeeRole.Dentist };
            var service = new PatientRecordService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.UpdatePatient(new Patient
            {
                Id = 1,
                FullName = "Nguyen Van Test",
                PhoneNumber = "0901234567",
                DateOfBirth = new DateTime(1990, 1, 1)
            }));

            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void InvoiceService_CancelInvoice_Receptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice { Id = 1, Status = InvoiceStatus.Pending });

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.CancelInvoice(1, "Sai hóa đơn"));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void InvoiceService_CancelInvoice_BlankReason_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice { Id = 1, Status = InvoiceStatus.Pending });

            var ex = Assert.ThrowsException<Exception>(() => service.CancelInvoice(1, " "));
            Assert.IsTrue(ex.Message.Contains("Lý do"));
        }

        [TestMethod]
        public void InvoiceService_CancelInvoice_Paid_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice { Id = 1, Status = InvoiceStatus.Paid });

            var ex = Assert.ThrowsException<Exception>(() => service.CancelInvoice(1, "Sai hóa đơn"));
            Assert.IsTrue(ex.Message.Contains("Chỉ hóa đơn chờ thanh toán"));
        }

        [TestMethod]
        public void InvoiceService_CancelInvoice_PendingByManager_CancelsAndLogsReason()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            var service = new InvoiceService(_fakeUow);
            var invoice = new Invoice { Id = 1, Status = InvoiceStatus.Pending, Notes = "Ghi chú cũ" };
            _fakeUow.Invoices.Add(invoice);

            service.CancelInvoice(1, "Khách yêu cầu hủy");

            Assert.AreEqual(InvoiceStatus.Cancelled, invoice.Status);
            Assert.IsTrue(invoice.Notes.Contains("Ghi chú cũ"));
            Assert.IsTrue(invoice.Notes.Contains("Khách yêu cầu hủy"));
            Assert.IsTrue(invoice.Notes.Contains("#1"));
        }

        [TestMethod]
        public void InvoiceService_PayInvoice_Cancelled_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice { Id = 1, Status = InvoiceStatus.Cancelled });

            var ex = Assert.ThrowsException<Exception>(() => service.PayInvoice(1));
            Assert.IsTrue(ex.Message.Contains("Chỉ hóa đơn chờ thanh toán"));
        }

        [TestMethod]
        public void InvoiceService_PayInvoice_Pending_ShouldSetPaidAmountAndStatus()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            var invoice = new Invoice
            {
                Id = 1,
                Status = InvoiceStatus.Pending,
                TotalAmount = 100000m,
                PaidAmount = 0m
            };
            _fakeUow.Invoices.Add(invoice);

            service.PayInvoice(1);

            Assert.AreEqual(100000m, invoice.PaidAmount);
            Assert.AreEqual(InvoiceStatus.Paid, invoice.Status);
        }

        [TestMethod]
        public void InvoiceService_AddPayment_Cancelled_ThrowsException()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            _fakeUow.Invoices.Add(new Invoice { Id = 1, Status = InvoiceStatus.Cancelled });

            var ex = Assert.ThrowsException<Exception>(() => service.AddPayment(1, 100000, "", 2));
            Assert.IsTrue(ex.Message.Contains("Chỉ hóa đơn chờ thanh toán"));
        }

        [TestMethod]
        public void InvoiceService_AddPayment_FullAmount_ShouldMarkPaid()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new InvoiceService(_fakeUow);
            var invoice = new Invoice
            {
                Id = 1,
                Status = InvoiceStatus.Pending,
                TotalAmount = 100000m,
                PaidAmount = 50000m
            };
            _fakeUow.Invoices.Add(invoice);

            service.AddPayment(1, 50000m, "Thanh toan du", 2);

            Assert.AreEqual(100000m, invoice.PaidAmount);
            Assert.AreEqual(InvoiceStatus.Paid, invoice.Status);
            Assert.AreEqual(1, _fakeUow.InvoicePayments.GetAll().Count());
        }

        [TestMethod]
        public void ServiceService_AddService_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new ServiceService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.AddService(new Service
            {
                Name = "Khám mới",
                Price = 100000
            }));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void ServiceService_SetServicePrice_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new ServiceService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.SetServicePrice(1, 100000, DateTime.Today.AddDays(1)));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void ServiceService_GetAllServices_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new ServiceService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetAllServices().ToList());
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void WorkShiftService_SaveWeeklySchedules_RoleDentist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 3, Role = EmployeeRole.Dentist };
            var service = new WorkShiftService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.SaveWeeklySchedules(new List<EmployeeSchedule>(), DateTime.Today, DateTime.Today.AddDays(6)));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void WorkShiftService_DeleteWorkShift_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new WorkShiftService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.DeleteWorkShift(1));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void EmployeeService_GetAllEmployees_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };
            var service = new EmployeeService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetAllEmployees());
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void InvoiceService_GetInvoiceById_RoleDentist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 3, Role = EmployeeRole.Dentist };
            var service = new InvoiceService(_fakeUow);

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => service.GetInvoiceById(1));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        private void SeedDentistWorkShift(int dentistId, DateTime date)
        {
            _fakeUow.Employees.Add(new Employee
            {
                Id = dentistId,
                Role = EmployeeRole.Dentist,
                ContractStatus = ContractStatus.Working
            });
            _fakeUow.WorkShifts.Add(new WorkShift
            {
                Id = dentistId,
                Name = $"Ca {dentistId}",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });
            _fakeUow.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = dentistId,
                WorkShiftId = dentistId,
                ScheduleDate = date.Date
            });
        }

        private Appointment CreateValidAppointment(DateTime start)
        {
            return new Appointment
            {
                PatientName = "Nguyen Van A",
                PhoneNumber = "0123456789",
                DentistId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Pending
            };
        }
    }
}
