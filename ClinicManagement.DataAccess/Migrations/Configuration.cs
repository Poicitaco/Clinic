namespace ClinicManagement.DataAccess.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using ClinicManagement.Core;

    internal sealed class Configuration : DbMigrationsConfiguration<ClinicManagement.DataAccess.ClinicDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
            SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.EntityFramework.MySqlMigrationSqlGenerator());
        }

        protected override void Seed(ClinicManagement.DataAccess.ClinicDbContext context)
        {
            // Seed Demo Employees
            var admin = new Employee { FullName = "Admin Quản Lý", DateOfBirth = new DateTime(1985, 1, 1), Gender = Gender.Male, PhoneNumber = "0123456789", Email = "admin@clinic.com", Role = EmployeeRole.Manager, Degree = AcademicDegree.Master, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };
            var dentist = new Employee { FullName = "Nha sĩ Nguyễn Văn A", DateOfBirth = new DateTime(1990, 5, 15), Gender = Gender.Male, PhoneNumber = "0987654321", Email = "dentist@clinic.com", Role = EmployeeRole.Dentist, Degree = AcademicDegree.Doctor, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };
            var dentist2 = new Employee { FullName = "Nha sĩ Trần Minh B", DateOfBirth = new DateTime(1988, 3, 10), Gender = Gender.Male, PhoneNumber = "0900000002", Email = "dentist2@clinic.com", Role = EmployeeRole.Dentist, Degree = AcademicDegree.Master, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };
            var dentist3 = new Employee { FullName = "Nha sĩ Lê Thu C", DateOfBirth = new DateTime(1992, 7, 22), Gender = Gender.Female, PhoneNumber = "0900000003", Email = "dentist3@clinic.com", Role = EmployeeRole.Dentist, Degree = AcademicDegree.Doctor, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };
            var dentist4 = new Employee { FullName = "Nha sĩ Phạm Quốc D", DateOfBirth = new DateTime(1987, 11, 5), Gender = Gender.Male, PhoneNumber = "0900000004", Email = "dentist4@clinic.com", Role = EmployeeRole.Dentist, Degree = AcademicDegree.PhD, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };
            var receptionist = new Employee { FullName = "Lễ tân Trần Thị B", DateOfBirth = new DateTime(1995, 10, 20), Gender = Gender.Female, PhoneNumber = "0912345678", Email = "receptionist@clinic.com", Role = EmployeeRole.Receptionist, Degree = AcademicDegree.None, ContractStatus = ContractStatus.Working, StartDate = DateTime.Now };

            context.Employees.AddOrUpdate(e => e.Email, admin, dentist, dentist2, dentist3, dentist4, receptionist);
            context.SaveChanges();

            // Seed Demo Accounts
            var adminId = context.Employees.First(e => e.Email == "admin@clinic.com").Id;
            var dentistId = context.Employees.First(e => e.Email == "dentist@clinic.com").Id;
            var dentist2Id = context.Employees.First(e => e.Email == "dentist2@clinic.com").Id;
            var dentist3Id = context.Employees.First(e => e.Email == "dentist3@clinic.com").Id;
            var dentist4Id = context.Employees.First(e => e.Email == "dentist4@clinic.com").Id;
            var receptionistId = context.Employees.First(e => e.Email == "receptionist@clinic.com").Id;

            // Password demo cho tất cả tài khoản seed: Manager@123
            string defaultPassword = "Manager@123";
            string defaultHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

            context.Accounts.AddOrUpdate(a => a.Username,
                new Account { Id = adminId, EmployeeId = adminId, Username = "admin@clinic.com", PasswordHash = defaultHash, IsActive = true },
                new Account { Id = dentistId, EmployeeId = dentistId, Username = "dentist@clinic.com", PasswordHash = defaultHash, IsActive = true },
                new Account { Id = dentist2Id, EmployeeId = dentist2Id, Username = "dentist2@clinic.com", PasswordHash = defaultHash, IsActive = true },
                new Account { Id = dentist3Id, EmployeeId = dentist3Id, Username = "dentist3@clinic.com", PasswordHash = defaultHash, IsActive = true },
                new Account { Id = dentist4Id, EmployeeId = dentist4Id, Username = "dentist4@clinic.com", PasswordHash = defaultHash, IsActive = true },
                new Account { Id = receptionistId, EmployeeId = receptionistId, Username = "receptionist@clinic.com", PasswordHash = defaultHash, IsActive = true }
            );

            // Seed Service Category
            context.ServiceCategories.AddOrUpdate(c => c.Name,
                new ServiceCategory { Name = "Khám bệnh" },
                new ServiceCategory { Name = "Điều trị" },
                new ServiceCategory { Name = "Thẩm mỹ" }
            );
            context.SaveChanges();

            var catKham = context.ServiceCategories.First(c => c.Name == "Khám bệnh").Id;
            var catDieuTri = context.ServiceCategories.First(c => c.Name == "Điều trị").Id;
            var catThamMy = context.ServiceCategories.First(c => c.Name == "Thẩm mỹ").Id;

            // Seed Services
            context.Services.AddOrUpdate(s => s.Name,
                new Service { Name = "Khám tổng quát", Description = "Khám và tư vấn răng miệng", Price = 100000, CategoryId = catKham, IsActive = true },
                new Service { Name = "Nhổ răng khôn", Description = "Tiểu phẫu nhổ răng khôn mọc lệch", Price = 1500000, CategoryId = catDieuTri, IsActive = true },
                new Service { Name = "Trám răng thẩm mỹ", Description = "Trám composite màu răng", Price = 300000, CategoryId = catThamMy, IsActive = true },
                new Service { Name = "Lấy cao răng", Description = "Cạo vôi răng và đánh bóng", Price = 150000, CategoryId = catKham, IsActive = true }
            );

            // Seed Patients
            context.Patients.AddOrUpdate(p => p.PhoneNumber,
                new Patient { FullName = "Bệnh nhân Lê Văn C", DateOfBirth = new DateTime(2000, 2, 2), Gender = Gender.Male, PhoneNumber = "0999888777", Address = "Hà Nội" },
                new Patient { FullName = "Bệnh nhân Phạm Thị D", DateOfBirth = new DateTime(1988, 8, 8), Gender = Gender.Female, PhoneNumber = "0888777666", Address = "Hồ Chí Minh" }
            );

            context.WorkShifts.AddOrUpdate(s => s.Name,
                new WorkShift { Name = "Ca sáng", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0) },
                new WorkShift { Name = "Ca chiều", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0) }
            );

            if (!context.SalaryConfigurations.Any())
            {
                var salaryConfig = new SalaryConfiguration
                {
                    HourlyRate = 120000,
                    DefaultShiftCoefficient = 1.0m,
                    ReceptionistCoefficient = 1.0m
                };

                context.SalaryConfigurations.Add(salaryConfig);
                context.SaveChanges();

                context.DentistDegreeSalaryCoefficients.AddOrUpdate(c => new { c.SalaryConfigurationId, c.Degree },
                    new DentistDegreeSalaryCoefficient { SalaryConfigurationId = salaryConfig.Id, Degree = AcademicDegree.Doctor, Coefficient = 1.0m },
                    new DentistDegreeSalaryCoefficient { SalaryConfigurationId = salaryConfig.Id, Degree = AcademicDegree.Master, Coefficient = 1.1m },
                    new DentistDegreeSalaryCoefficient { SalaryConfigurationId = salaryConfig.Id, Degree = AcademicDegree.PhD, Coefficient = 1.2m },
                    new DentistDegreeSalaryCoefficient { SalaryConfigurationId = salaryConfig.Id, Degree = AcademicDegree.AssociateProfessor, Coefficient = 1.3m },
                    new DentistDegreeSalaryCoefficient { SalaryConfigurationId = salaryConfig.Id, Degree = AcademicDegree.Professor, Coefficient = 1.4m }
                );
            }

            context.SaveChanges();

            var today = DateTime.Today;
            var weekStart = today.AddDays(-1 * ((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7)).Date;
            var weekEnd = weekStart.AddDays(6);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var cleanupStart = weekStart < monthStart ? weekStart : monthStart;
            var cleanupEnd = weekEnd > monthEnd ? weekEnd : monthEnd;

            var existingDemoSchedules = context.EmployeeSchedules
                .Where(s => s.ScheduleDate >= cleanupStart && s.ScheduleDate <= cleanupEnd)
                .ToList();
            context.EmployeeSchedules.RemoveRange(existingDemoSchedules);
            context.SaveChanges();

            {
                var demoDentistIds = context.Employees
                    .Where(e => e.Role == EmployeeRole.Dentist && e.ContractStatus == ContractStatus.Working)
                    .OrderBy(e => e.Id)
                    .Take(4)
                    .Select(e => e.Id)
                    .ToList();

                var demoReceptionistId = context.Employees
                    .Where(e => e.Role == EmployeeRole.Receptionist && e.ContractStatus == ContractStatus.Working)
                    .OrderBy(e => e.Id)
                    .Select(e => e.Id)
                    .FirstOrDefault();

                var demoShiftIds = context.WorkShifts
                    .OrderBy(s => s.StartTime)
                    .Select(s => s.Id)
                    .Take(2)
                    .ToList();

                if (demoDentistIds.Count >= 4 && demoReceptionistId > 0 && demoShiftIds.Count >= 2)
                {
                    for (int day = 0; day < 7; day++)
                    {
                        var scheduleDate = weekStart.AddDays(day);
                        foreach (var shiftId in demoShiftIds)
                        {
                            context.EmployeeSchedules.Add(new EmployeeSchedule { EmployeeId = demoReceptionistId, WorkShiftId = shiftId, ScheduleDate = scheduleDate, ShiftCoefficient = 1.0f, PatientCoefficient = 0f });
                            foreach (var dentistEmpId in demoDentistIds)
                            {
                                context.EmployeeSchedules.Add(new EmployeeSchedule { EmployeeId = dentistEmpId, WorkShiftId = shiftId, ScheduleDate = scheduleDate, ShiftCoefficient = 1.0f, PatientCoefficient = 0.1f });
                            }
                        }
                    }
                }
            }

            context.SaveChanges();

            {
                var payrollMonth = DateTime.Today.Month;
                var payrollYear = DateTime.Today.Year;
                var payrollStart = new DateTime(payrollYear, payrollMonth, 1);
                var payrollEnd = payrollStart.AddMonths(1).AddDays(-1);

                var existingPayroll = context.SalaryRecords
                    .Where(r => r.Month == payrollMonth && r.Year == payrollYear)
                    .ToList();

                if (existingPayroll.Any())
                {
                    var payrollIds = existingPayroll.Select(r => r.Id).ToList();
                    var existingSnapshots = context.SalaryFormulaSnapshots
                        .Where(s => payrollIds.Contains(s.SalaryRecordId))
                        .ToList();

                    context.SalaryFormulaSnapshots.RemoveRange(existingSnapshots);
                    context.SalaryRecords.RemoveRange(existingPayroll);
                    context.SaveChanges();
                }

                var salaryConfig = context.SalaryConfigurations.FirstOrDefault();
                var shifts = context.WorkShifts.ToDictionary(s => s.Id);
                var schedules = context.EmployeeSchedules
                    .Where(s => s.ScheduleDate >= payrollStart && s.ScheduleDate <= payrollEnd)
                    .ToList();

                var payrollEmployees = context.Employees
                    .Where(e => e.ContractStatus == ContractStatus.Working
                        && (e.Role == EmployeeRole.Dentist || e.Role == EmployeeRole.Receptionist))
                    .OrderBy(e => e.Id)
                    .ToList();

                foreach (var employee in payrollEmployees)
                {
                    var totalHours = schedules
                        .Where(s => s.EmployeeId == employee.Id && shifts.ContainsKey(s.WorkShiftId))
                        .Sum(s => (decimal)(shifts[s.WorkShiftId].EndTime - shifts[s.WorkShiftId].StartTime).TotalHours);

                    context.SalaryRecords.Add(new SalaryRecord
                    {
                        EmployeeId = employee.Id,
                        Month = payrollMonth,
                        Year = payrollYear,
                        TotalHours = totalHours,
                        TotalAmount = totalHours * (salaryConfig?.HourlyRate ?? 0m),
                        IsFinalized = false
                    });
                }

                context.SaveChanges();
            }

            if (!context.Appointments.Any())
            {
                var firstDentistId = context.Employees.First(e => e.Email == "dentist@clinic.com").Id;
                var receptionistEmployeeId = context.Employees.First(e => e.Email == "receptionist@clinic.com").Id;
                var appointmentDate = DateTime.Today.AddDays(1).Date.AddHours(9);

                context.Appointments.Add(new Appointment
                {
                    PatientName = "Bệnh nhân Lê Văn C",
                    PhoneNumber = "0999888777",
                    DentistId = firstDentistId,
                    StartTime = appointmentDate,
                    EndTime = appointmentDate.AddHours(1),
                    Status = AppointmentStatus.Pending,
                    CreatedById = receptionistEmployeeId
                });

                context.Appointments.Add(new Appointment
                {
                    PatientName = "Bệnh nhân Phạm Thị D",
                    PhoneNumber = "0888777666",
                    DentistId = firstDentistId,
                    StartTime = appointmentDate.AddHours(2),
                    EndTime = appointmentDate.AddHours(3),
                    Status = AppointmentStatus.Completed,
                    CreatedById = receptionistEmployeeId
                });

                context.SaveChanges();
            }

            if (!context.PatientExaminations.Any())
            {
                var patient = context.Patients.First(p => p.PhoneNumber == "0999888777");
                var dentistEmployeeId = context.Employees.First(e => e.Email == "dentist@clinic.com").Id;
                var completedAppointment = context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderBy(a => a.StartTime)
                    .FirstOrDefault();

                context.PatientExaminations.Add(new PatientExamination
                {
                    PatientId = patient.Id,
                    AppointmentId = completedAppointment?.Id,
                    DentistId = dentistEmployeeId,
                    ExaminationDate = DateTime.Today.AddDays(-1).Date.AddHours(10),
                    Symptoms = "Đau răng hàm, ê buốt khi ăn lạnh",
                    Diagnosis = "Sâu răng mức độ nhẹ",
                    TreatmentPlan = "Trám răng và tái khám sau 2 tuần",
                    Prescription = "Paracetamol 500mg khi đau",
                    DentalChartDetails = "Răng 14: sâu nhẹ; Răng 15: theo dõi",
                    Notes = "Demo bệnh án đã chốt để lập hóa đơn",
                    Status = ExaminationStatus.Finalized
                });

                context.SaveChanges();
            }

            if (!context.Invoices.Any())
            {
                var finalizedExam = context.PatientExaminations
                    .Where(e => e.Status == ExaminationStatus.Finalized)
                    .OrderByDescending(e => e.ExaminationDate)
                    .First();
                var receptionistEmployeeId = context.Employees.First(e => e.Email == "receptionist@clinic.com").Id;
                var service = context.Services.First(s => s.Name == "Trám răng thẩm mỹ");

                var invoice = new Invoice
                {
                    PatientId = finalizedExam.PatientId,
                    ExaminationId = finalizedExam.Id,
                    CreatedDate = DateTime.Now,
                    CreatedById = receptionistEmployeeId,
                    Notes = "Hóa đơn demo từ bệnh án mẫu",
                    TotalAmount = service.Price,
                    PaidAmount = service.Price,
                    Status = InvoiceStatus.Paid
                };

                context.Invoices.Add(invoice);
                context.SaveChanges();

                context.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoice.Id,
                    ServiceId = service.Id,
                    Quantity = 1,
                    UnitPrice = service.Price,
                    Amount = service.Price
                });

                context.InvoicePayments.Add(new InvoicePayment
                {
                    InvoiceId = invoice.Id,
                    Amount = service.Price,
                    PaymentDate = DateTime.Now,
                    CreatedById = receptionistEmployeeId,
                    Note = "Thanh toán demo"
                });

                context.SaveChanges();
            }
        }
    }
}
