using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ClinicManagement.Core;
using ClinicManagement.Business;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class AuthenticationFlowTests
    {
        private FakeUnitOfWork _fakeUow;
        private EmployeeService _employeeService;

        [TestInitialize]
        public void Setup()
        {
            _fakeUow = new FakeUnitOfWork();
            _employeeService = new EmployeeService(_fakeUow);

            // Add a Manager for testing login
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Manager@123");

            var manager = new Employee 
            { 
                Id = 1, 
                Email = "manager@clinic.com",
                Role = EmployeeRole.Manager, 
                ContractStatus = ContractStatus.Working 
            };
            
            manager.Account = new Account
            {
                Id = 1,
                EmployeeId = 1,
                Employee = manager,
                Username = "manager@clinic.com",
                PasswordHash = hashedPassword,
                IsActive = true
            };

            var dentist = new Employee 
            { 
                Id = 2, 
                Email = "dentist@clinic.com",
                Role = EmployeeRole.Dentist, 
                ContractStatus = ContractStatus.Working 
            };
            
            dentist.Account = new Account
            {
                Id = 2,
                EmployeeId = 2,
                Employee = dentist,
                Username = "dentist@clinic.com",
                PasswordHash = hashedPassword,
                IsActive = true
            };

            _fakeUow.Employees.Add(manager);
            _fakeUow.Accounts.Add(manager.Account);
            
            _fakeUow.Employees.Add(dentist);
            _fakeUow.Accounts.Add(dentist.Account);

            // Ensure Context is completely null before starting
            UserContext.CurrentUser = null;
        }

        [TestMethod]
        public void Test_Login_SetsContext_Correctly()
        {
            // Simulate the exact flow in LoginViewModel.Login()
            
            // 1. Initial State: Nobody logged in
            Assert.IsNull(UserContext.CurrentUser);

            // 2. Action: Login via Service
            var loggedInEmployee = _employeeService.Login("manager@clinic.com", "Manager@123");
            Assert.IsNotNull(loggedInEmployee);

            // 3. Action: UI ViewModel explicitly sets Context (like the updated LoginViewModel)
            UserContext.CurrentUser = loggedInEmployee;

            // 4. Verify: Context is correctly set
            Assert.AreEqual(1, UserContext.CurrentUser.Id);
            Assert.AreEqual(EmployeeRole.Manager, UserContext.CurrentUser.Role);
        }

        [TestMethod]
        public void Test_Logout_ClearsContext_DeniesAccess()
        {
            // Mô phỏng kịch bản thực tế của UI:
            
            // 1. Đăng nhập với tư cách Manager (giống LoginViewModel.Login)
            var managerLogin = _employeeService.Login("manager@clinic.com", "Manager@123");
            UserContext.CurrentUser = managerLogin;
            
            // Xác nhận Context đúng là Manager
            Assert.IsNotNull(UserContext.CurrentUser);
            Assert.AreEqual(EmployeeRole.Manager, UserContext.CurrentUser.Role);

            // Thử gọi chức năng Manager: CreateEmployee (Phải thành công, không văng lỗi)
            var newEmp = new Employee { FullName = "Nguyễn Văn A", Email = "a@clinic.com", PhoneNumber = "0123456789", Role = EmployeeRole.Dentist, Degree = AcademicDegree.Doctor, DateOfBirth = DateTime.Today.AddYears(-25), StartDate = DateTime.Today };
            _employeeService.CreateEmployee(newEmp); 

            // 2. Gọi Logout (giống MainWindow.xaml.cs Logout_Click)
            UserContext.CurrentUser = null;
            
            // Xác nhận Context đã bị xóa
            Assert.IsNull(UserContext.CurrentUser);

            // 3. Đăng nhập lại với tư cách Nha sĩ (cùng phiên)
            var dentistLogin = _employeeService.Login("dentist@clinic.com", "Manager@123");
            UserContext.CurrentUser = dentistLogin; 

            // Xác nhận Context đúng là Dentist, KHÔNG còn là Manager
            Assert.IsNotNull(UserContext.CurrentUser);
            Assert.AreEqual(EmployeeRole.Dentist, UserContext.CurrentUser.Role);
            Assert.AreNotEqual(EmployeeRole.Manager, UserContext.CurrentUser.Role);

            // 4. Thử gọi chức năng Manager: CreateEmployee (Phải throw Unauthorized)
            var newEmp2 = new Employee { FullName = "Trần Thị B", Email = "b@clinic.com", PhoneNumber = "0987654321", Role = EmployeeRole.Dentist, Degree = AcademicDegree.Doctor, DateOfBirth = DateTime.Today.AddYears(-25), StartDate = DateTime.Today };
            
            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => _employeeService.CreateEmployee(newEmp2));
            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void Login_ResignationDateToday_DisablesAccountAndRejectsLogin()
        {
            var dentist = _fakeUow.Employees.GetById(2);
            dentist.ResignationDate = DateTime.Today;

            var ex = Assert.ThrowsException<Exception>(() => _employeeService.Login("dentist@clinic.com", "Manager@123"));

            Assert.AreEqual("Tài khoản đã bị vô hiệu hóa.", ex.Message);
            Assert.AreEqual(ContractStatus.Resigned, dentist.ContractStatus);
            Assert.IsFalse(dentist.Account.IsActive);
        }
    }
}
