using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _email;
        private string _password;
        private string _errorMessage;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public Action LoginSuccess { get; set; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login());
        }

        private void Login()
        {
            ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                MessageBox.Show(ErrorMessage, "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var context = new ClinicDbContext();
                var unitOfWork = new UnitOfWork(context);
                var employeeService = new Business.EmployeeService(unitOfWork);

                var employee = employeeService.Login(Email, Password);

                // Login successful!
                UserContext.CurrentUser = employee;

                MessageBox.Show($"Đăng nhập thành công với vai trò {GetRoleName(employee.Role)}.", "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoginSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                MessageBox.Show($"Đăng nhập thất bại: {ErrorMessage}", "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRoleName(EmployeeRole role)
        {
            switch (role)
            {
                case EmployeeRole.Manager:
                    return "Quản lý";
                case EmployeeRole.Dentist:
                    return "Nha sĩ";
                case EmployeeRole.Receptionist:
                    return "Lễ tân";
                default:
                    return role.ToString();
            }
        }
    }
}
