using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using ClinicManagement.Core;
using ClinicManagement.Business;

using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.DataAccess.Repositories;
using System.Linq.Expressions;

namespace ClinicManagement.Tests
{
    // Removed legacy fake implementations

    [TestClass]
    public class EmployeeServiceTests
    {
        private EmployeeService _employeeService;
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            _fakeUow = new FakeUnitOfWork();
            _employeeService = new EmployeeService(_fakeUow);
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Manager };
        }

        [TestMethod]
        public void CreateEmployee_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var newEmployee = new Employee
            {
                FullName = "Nguyen Van A",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                PhoneNumber = "0987654321",
                Email = "test.creation@gmail.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };

            // Act
            var created = _employeeService.CreateEmployee(newEmployee);

            // Assert
            Assert.IsNotNull(created);
            Assert.IsTrue(created.Id > 0);
            Assert.IsNotNull(created.Account);
            Assert.AreEqual("test.creation@gmail.com", created.Account.Username);
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("01011990Aa@", created.Account.PasswordHash));
        }

        [TestMethod]
        public void CreateEmployee_Under18_ShouldThrowException()
        {
            var underAgeEmployee = new Employee
            {
                FullName = "Tran B",
                DateOfBirth = DateTime.Now.AddYears(-17),
                PhoneNumber = "0123456789",
                Email = "underage@gmail.com",
                Role = EmployeeRole.Receptionist,
                StartDate = DateTime.Now
            };

            var ex = Assert.ThrowsException<ArgumentException>(() => _employeeService.CreateEmployee(underAgeEmployee));
            Assert.AreEqual("Nhân viên phải từ 18 tuổi trở lên.", ex.Message);
        }

        [TestMethod]
        public void CreateEmployee_Receptionist_ShouldNullifyDegree()
        {
            var receptionist = new Employee
            {
                FullName = "Le Thi C",
                DateOfBirth = new DateTime(1995, 5, 5),
                PhoneNumber = "0999888777",
                Email = "receptionist_test.creation@gmail.com",
                Role = EmployeeRole.Receptionist,
                Degree = AcademicDegree.Doctor, // Deliberately set
                StartDate = DateTime.Now
            };

            var created = _employeeService.CreateEmployee(receptionist);
            Assert.IsNull(created.Degree);
        }

        [TestMethod]
        public void TerminateContract_Today_ShouldInactiveImmediately()
        {
            // Arrange
            var emp = new Employee
            {
                FullName = "Nguyen Van D",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                PhoneNumber = "0987654321",
                Email = "email@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now.AddYears(-1)
            };
            var created = _employeeService.CreateEmployee(emp);

            // Act
            _employeeService.TerminateContract(created.Id, DateTime.Now);

            // Assert
            Assert.AreEqual(ContractStatus.Resigned, created.ContractStatus);
            Assert.IsFalse(created.Account.IsActive);
        }

        [TestMethod]
        public void TerminateContract_Future_ShouldKeepActiveButSetDate()
        {
            // Arrange
            var emp = new Employee
            {
                FullName = "Nguyen Van E",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0912121212",
                Email = "emailE@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now.AddYears(-1)
            };
            var created = _employeeService.CreateEmployee(emp);

            var futureDate = DateTime.Now.AddDays(5);

            // Act
            _employeeService.TerminateContract(created.Id, futureDate);

            // Assert
            Assert.AreEqual(ContractStatus.Working, created.ContractStatus); // Should remain working
            Assert.IsTrue(created.Account.IsActive); // Should remain active
            Assert.AreEqual(futureDate, created.ResignationDate);
        }

        [TestMethod]
        public void TerminateContract_Twice_ShouldThrowException()
        {
            // Arrange
            var emp = new Employee
            {
                FullName = "Nguyen Van F",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0913131313",
                Email = "emailF@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now.AddYears(-1)
            };
            var created = _employeeService.CreateEmployee(emp);

            // First termination (future context)
            _employeeService.TerminateContract(created.Id, DateTime.Now.AddDays(5));

            // Act & Assert
            var ex = Assert.ThrowsException<Exception>(() => _employeeService.TerminateContract(created.Id, DateTime.Now));
            Assert.AreEqual("Nhân viên này đã có quyết định thôi việc, không thể chấm dứt hợp đồng lần nữa.", ex.Message);
        }

        [TestMethod]
        public void TerminateContract_DateBeforeStartDate_ShouldThrowException()
        {
            var emp = new Employee
            {
                FullName = "Nguyen Van G",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0914141414",
                Email = "emailG@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };
            var created = _employeeService.CreateEmployee(emp);

            var ex = Assert.ThrowsException<Exception>(() => _employeeService.TerminateContract(created.Id, DateTime.Now.AddDays(-1)));
            Assert.AreEqual("Ngày thôi việc không được nhỏ hơn ngày bắt đầu làm việc.", ex.Message);
        }

        [TestMethod]
        public void UpdateEmployee_SetStatusResigned_ShouldInactiveImmediately()
        {
            var emp = new Employee
            {
                FullName = "Nguyen Van U",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                PhoneNumber = "0922222222",
                Email = "emailU@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now.AddYears(-1)
            };
            var created = _employeeService.CreateEmployee(emp);

            _employeeService.UpdateEmployee(new Employee
            {
                Id = created.Id,
                FullName = created.FullName,
                DateOfBirth = created.DateOfBirth,
                Gender = created.Gender,
                PhoneNumber = created.PhoneNumber,
                Email = created.Email,
                Role = created.Role,
                Degree = created.Degree,
                StartDate = created.StartDate,
                ContractStatus = ContractStatus.Resigned
            });

            Assert.AreEqual(ContractStatus.Resigned, created.ContractStatus);
            Assert.AreEqual(DateTime.Today, created.ResignationDate.Value.Date);
            Assert.IsFalse(created.Account.IsActive);
        }

        [TestMethod]
        public void UpdateEmployee_SetStatusWorkingAfterResigned_ShouldReactivateAccount()
        {
            var emp = new Employee
            {
                FullName = "Nguyen Van V",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                PhoneNumber = "0933333333",
                Email = "emailV@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now.AddYears(-1)
            };
            var created = _employeeService.CreateEmployee(emp);
            _employeeService.TerminateContract(created.Id, DateTime.Today);

            _employeeService.UpdateEmployee(new Employee
            {
                Id = created.Id,
                FullName = created.FullName,
                DateOfBirth = created.DateOfBirth,
                Gender = created.Gender,
                PhoneNumber = created.PhoneNumber,
                Email = created.Email,
                Role = created.Role,
                Degree = created.Degree,
                StartDate = created.StartDate,
                ContractStatus = ContractStatus.Working
            });

            Assert.AreEqual(ContractStatus.Working, created.ContractStatus);
            Assert.IsNull(created.ResignationDate);
            Assert.IsTrue(created.Account.IsActive);
        }

        [TestMethod]
        public void CreateEmployee_DuplicateEmail_ShouldThrowException()
        {
            var emp1 = new Employee
            {
                FullName = "Nguyen Van H",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0915151515",
                Email = "duplicate@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };
            _employeeService.CreateEmployee(emp1);

            var emp2 = new Employee
            {
                FullName = "Nguyen Van I",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0916161616", // Different phone
                Email = "duplicate@test.com", // Same email
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };

            var ex = Assert.ThrowsException<ArgumentException>(() => _employeeService.CreateEmployee(emp2));
            Assert.AreEqual("Email này đã được sử dụng bởi nhân viên khác.", ex.Message);
        }

        [TestMethod]
        public void CreateEmployee_DuplicatePhone_ShouldThrowException()
        {
            var emp1 = new Employee
            {
                FullName = "Nguyen Van J",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0917171717",
                Email = "J@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };
            _employeeService.CreateEmployee(emp1);

            var emp2 = new Employee
            {
                FullName = "Nguyen Van K",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0917171717", // Same phone
                Email = "K@test.com", // Different email
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };

            var ex = Assert.ThrowsException<ArgumentException>(() => _employeeService.CreateEmployee(emp2));
            Assert.AreEqual("Số điện thoại này đã được sử dụng bởi nhân viên khác.", ex.Message);
        }

        [TestMethod]
        public void GenerateDefaultPassword_SingleDigitDayMonth_ShouldFormatCorrectly()
        {
            // Input: Employee with DateOfBirth 5/3/1998
            // Expected CHÍNH XÁC: PasswordHash matches "05031998Aa@"
            var emp = new Employee
            {
                FullName = "Nguyen Van L",
                DateOfBirth = new DateTime(1998, 3, 5), // 5th of March
                PhoneNumber = "0918181818",
                Email = "L@test.com",
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                StartDate = DateTime.Now
            };
            var created = _employeeService.CreateEmployee(emp);

            // Should be 05031998Aa@ (05 day, 03 month)
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("05031998Aa@", created.Account.PasswordHash));
        }

        [TestMethod]
        public void CreateEmployee_AvatarOver5Mb_ShouldThrowException()
        {
            var avatarPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".jpg");
            try
            {
                System.IO.File.WriteAllBytes(avatarPath, new byte[(5 * 1024 * 1024) + 1]);
                var emp = new Employee
                {
                    FullName = "Nguyen Van M",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    PhoneNumber = "0919191919",
                    Email = "M@test.com",
                    Role = EmployeeRole.Dentist,
                    Degree = AcademicDegree.Master,
                    StartDate = DateTime.Now,
                    AvatarUrl = avatarPath
                };

                var ex = Assert.ThrowsException<ArgumentException>(() => _employeeService.CreateEmployee(emp));
                Assert.AreEqual("Ảnh đại diện không được vượt quá 5MB.", ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(avatarPath))
                    System.IO.File.Delete(avatarPath);
            }
        }

        [TestMethod]
        public void EmployeeService_ShouldNotHaveDeleteMethod()
        {
            // Input: Type inspection of IEmployeeService
            // Expected CHÍNH XÁC: No method named "Delete" or "DeleteEmployee" exists.
            var type = typeof(ClinicManagement.Business.IEmployeeService);
            var methods = type.GetMethods().Select(m => m.Name);
            
            Assert.IsFalse(methods.Contains("Delete"), "IEmployeeService should not have a Delete method.");
            Assert.IsFalse(methods.Contains("DeleteEmployee"), "IEmployeeService should not have a DeleteEmployee method.");
            Assert.IsFalse(methods.Contains("RemoveEmployee"), "IEmployeeService should not have a RemoveEmployee method.");
        }

        [TestMethod]
        public void EmployeeService_ShouldNotExposeUpdateProfile()
        {
            var interfaceMethods = typeof(ClinicManagement.Business.IEmployeeService).GetMethods().Select(m => m.Name);
            var serviceMethods = typeof(EmployeeService).GetMethods().Select(m => m.Name);

            Assert.IsFalse(interfaceMethods.Contains("UpdateProfile"));
            Assert.IsFalse(serviceMethods.Contains("UpdateProfile"));
        }

        [TestMethod]
        public void ChangeLoginCredentials_Receptionist_ThrowsUnauthorized()
        {
            _fakeUow.Employees.Add(CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!"));
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Receptionist };

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                _employeeService.ChangeLoginCredentials(1, "new@test.com", "", ""));

            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void ChangeLoginCredentials_BlankPassword_UpdatesUsernameOnly()
        {
            var employee = CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!");
            var oldHash = employee.Account.PasswordHash;
            _fakeUow.Employees.Add(employee);

            _employeeService.ChangeLoginCredentials(1, "new@test.com", "", "");

            Assert.AreEqual("new@test.com", employee.Account.Username);
            Assert.AreEqual(oldHash, employee.Account.PasswordHash);
        }

        [TestMethod]
        public void ChangeLoginCredentials_NewPasswordRequiresMatchingConfirm()
        {
            _fakeUow.Employees.Add(CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!"));

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                _employeeService.ChangeLoginCredentials(1, "old@test.com", "NewPass1!", "Mismatch1!"));

            Assert.AreEqual("Xác nhận mật khẩu không khớp.", ex.Message);
        }

        [TestMethod]
        public void ChangeLoginCredentials_DuplicateUsername_ThrowsException()
        {
            _fakeUow.Employees.Add(CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!"));
            _fakeUow.Employees.Add(CreateEmployeeWithAccount(2, "taken@test.com", "OldPass1!"));

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                _employeeService.ChangeLoginCredentials(1, "taken@test.com", "", ""));

            Assert.AreEqual("Username đã tồn tại.", ex.Message);
        }

        [TestMethod]
        public void ChangeLoginCredentials_ValidPassword_HashesPassword()
        {
            var employee = CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!");
            _fakeUow.Employees.Add(employee);

            _employeeService.ChangeLoginCredentials(1, "old@test.com", "NewPass1!", "NewPass1!");

            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("NewPass1!", employee.Account.PasswordHash));
        }

        [TestMethod]
        public void RequestPasswordReset_ActiveAccount_ReturnsTokenAndSets15MinuteExpiry()
        {
            var employee = CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!");
            _fakeUow.Accounts.Add(employee.Account);
            var before = DateTime.Now.AddMinutes(14);

            var token = _employeeService.RequestPasswordReset("old@test.com");

            Assert.IsFalse(string.IsNullOrWhiteSpace(token));
            Assert.IsFalse(string.IsNullOrWhiteSpace(employee.Account.PasswordResetTokenHash));
            Assert.IsTrue(employee.Account.PasswordResetTokenExpiresAt >= before);
            Assert.IsTrue(employee.Account.PasswordResetTokenExpiresAt <= DateTime.Now.AddMinutes(16));
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify(token, employee.Account.PasswordResetTokenHash));
        }

        [TestMethod]
        public void ResetPassword_ValidToken_ChangesPasswordAndClearsToken()
        {
            var employee = CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!");
            _fakeUow.Accounts.Add(employee.Account);
            var token = _employeeService.RequestPasswordReset("old@test.com");

            _employeeService.ResetPassword(token, "NewPass1!", "NewPass1!");

            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("NewPass1!", employee.Account.PasswordHash));
            Assert.IsNull(employee.Account.PasswordResetTokenHash);
            Assert.IsNull(employee.Account.PasswordResetTokenExpiresAt);
        }

        [TestMethod]
        public void ResetPassword_ExpiredToken_ThrowsException()
        {
            var employee = CreateEmployeeWithAccount(1, "old@test.com", "OldPass1!");
            employee.Account.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword("token");
            employee.Account.PasswordResetTokenExpiresAt = DateTime.Now.AddMinutes(-1);
            _fakeUow.Accounts.Add(employee.Account);

            var ex = Assert.ThrowsException<Exception>(() =>
                _employeeService.ResetPassword("token", "NewPass1!", "NewPass1!"));

            Assert.IsTrue(ex.Message.Contains("hết hạn"));
        }

        private Employee CreateEmployeeWithAccount(int id, string username, string password)
        {
            return new Employee
            {
                Id = id,
                FullName = "Nguyen Van A",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "0987654321",
                Email = username,
                Role = EmployeeRole.Dentist,
                Degree = AcademicDegree.Master,
                Account = new Account
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    IsActive = true
                }
            };
        }
    }
}
