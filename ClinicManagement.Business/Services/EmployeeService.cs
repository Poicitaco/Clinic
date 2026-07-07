using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.Entity;
using System.Security.Cryptography;
using ClinicManagement.Core;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;

        // Constructor for production overriding / DI
        public EmployeeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Employee CreateEmployee(Employee employee)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            ValidateEmployee(employee, true);

            // Default account setup
            string defaultPassword = GenerateDefaultPassword(employee.DateOfBirth);

            var account = new Account
            {
                Username = employee.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                IsActive = true
            };

            employee.Account = account;
            employee.ContractStatus = ContractStatus.Working;

            _unitOfWork.Employees.Add(employee);
            _unitOfWork.Complete();

            return employee;
        }

        public Employee UpdateEmployee(Employee employee)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var existing = _unitOfWork.Employees.GetById(employee.Id);
            if (existing == null) throw new Exception("Không tìm thấy nhân viên.");

            ValidateEmployee(employee, false);

            existing.FullName = employee.FullName;
            existing.DateOfBirth = employee.DateOfBirth;
            existing.Gender = employee.Gender;
            existing.PhoneNumber = employee.PhoneNumber;

            // If email changed, update username as well
            if (existing.Email != employee.Email)
            {
                existing.Email = employee.Email;
                if (existing.Account != null)
                {
                    existing.Account.Username = employee.Email;
                }
            }

            existing.Address = employee.Address;
            existing.AvatarUrl = employee.AvatarUrl;

            // Update role specific logic
            existing.Role = employee.Role;
            if (existing.Role == EmployeeRole.Receptionist)
            {
                existing.Degree = null; // Always null for receptionist
            }
            else
            {
                existing.Degree = employee.Degree;
            }

            if (employee.ContractStatus == ContractStatus.Resigned
                && existing.ContractStatus != ContractStatus.Resigned
                && !existing.ResignationDate.HasValue)
            {
                if (DateTime.Today < existing.StartDate.Date)
                    throw new Exception("Ngày thôi việc không được nhỏ hơn ngày bắt đầu làm việc.");

                existing.ResignationDate = DateTime.Today;
                existing.ContractStatus = ContractStatus.Resigned;
                if (existing.Account != null)
                {
                    existing.Account.IsActive = false;
                }
            }
            else if (employee.ContractStatus == ContractStatus.Working
                && (existing.ContractStatus == ContractStatus.Resigned || existing.ResignationDate.HasValue))
            {
                existing.ContractStatus = ContractStatus.Working;
                existing.ResignationDate = null;
                if (existing.Account != null)
                {
                    existing.Account.IsActive = true;
                }
            }

            _unitOfWork.Complete();
            return existing;
        }

        public void TerminateContract(int employeeId, DateTime resignationDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var employee = _unitOfWork.Employees.GetById(employeeId);
            if (employee == null) throw new Exception("Không tìm thấy nhân viên.");

            if (employee.ContractStatus == ContractStatus.Resigned || employee.ResignationDate.HasValue)
                throw new Exception("Nhân viên này đã có quyết định thôi việc, không thể chấm dứt hợp đồng lần nữa.");

            if (resignationDate < employee.StartDate)
                throw new Exception("Ngày thôi việc không được nhỏ hơn ngày bắt đầu làm việc.");

            employee.ResignationDate = resignationDate;

            // If resignation date is today or in the past, inactive immediately
            if (resignationDate.Date <= DateTime.Now.Date)
            {
                employee.ContractStatus = ContractStatus.Resigned;
                if (employee.Account != null)
                {
                    employee.Account.IsActive = false;
                }
            }
            else 
            {
                // Future resignation: Requires a background job to process later. 
                // For now we just set the resignation date.
            }

            _unitOfWork.Complete();
        }

        public void ChangeLoginCredentials(int employeeId, string username, string newPassword, string confirmPassword)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var employee = _unitOfWork.Employees.GetById(employeeId);
            if (employee == null || employee.Account == null) 
                throw new Exception("Không tìm thấy tài khoản nhân viên.");

            if (string.IsNullOrWhiteSpace(username) || !Regex.IsMatch(username, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Username không đúng định dạng email.");

            var duplicateUsername = _unitOfWork.Employees.AsQueryable()
                .Any(e => e.Id != employeeId && e.Account != null && e.Account.Username == username);
            if (duplicateUsername)
                throw new ArgumentException("Username đã tồn tại.");

            employee.Account.Username = username;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword != confirmPassword)
                    throw new ArgumentException("Xác nhận mật khẩu không khớp.");

                ValidatePasswordFormat(newPassword);
                employee.Account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            _unitOfWork.Complete();
        }

        public bool ChangePassword(int employeeId, string oldPassword, string newPassword)
        {
            var employee = _unitOfWork.Employees.GetById(employeeId);
            if (employee == null || employee.Account == null) 
                throw new Exception("Không tìm thấy tài khoản nhân viên.");

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, employee.Account.PasswordHash))
            {
                return false;
            }

            ValidatePasswordFormat(newPassword);

            employee.Account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _unitOfWork.Complete();
            return true;
        }

        public string RequestPasswordReset(string username)
        {
            var account = _unitOfWork.Accounts.GetAll()
                .FirstOrDefault(a => a.Username == username && a.IsActive);
            if (account == null)
                return null;

            var token = GenerateResetToken();
            account.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
            account.PasswordResetTokenExpiresAt = DateTime.Now.AddMinutes(15);
            _unitOfWork.Complete();
            return token;
        }

        public void ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token reset mật khẩu không hợp lệ.");

            if (newPassword != confirmPassword)
                throw new ArgumentException("Xác nhận mật khẩu không khớp.");

            ValidatePasswordFormat(newPassword);

            var now = DateTime.Now;
            var account = _unitOfWork.Accounts.GetAll()
                .FirstOrDefault(a => a.PasswordResetTokenExpiresAt.HasValue
                    && a.PasswordResetTokenExpiresAt.Value >= now
                    && !string.IsNullOrWhiteSpace(a.PasswordResetTokenHash)
                    && BCrypt.Net.BCrypt.Verify(token, a.PasswordResetTokenHash));

            if (account == null)
                throw new Exception("Token reset mật khẩu không hợp lệ hoặc đã hết hạn.");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            account.PasswordResetTokenHash = null;
            account.PasswordResetTokenExpiresAt = null;
            _unitOfWork.Complete();
        }

        public Employee Login(string username, string password)
        {
            var account = _unitOfWork.Accounts.AsQueryable().Include("Employee")
                        .FirstOrDefault(a => a.Username == username);

            if (account == null)
            {
                throw new Exception("Email hoặc mật khẩu không đúng.");
            }

            if (!account.IsActive)
            {
                throw new Exception("Tài khoản đã bị vô hiệu hóa.");
            }

            if (account.Employee != null
                && account.Employee.ResignationDate.HasValue
                && account.Employee.ResignationDate.Value.Date <= DateTime.Today)
            {
                account.Employee.ContractStatus = ContractStatus.Resigned;
                account.IsActive = false;
                _unitOfWork.Complete();
                throw new Exception("Tài khoản đã bị vô hiệu hóa.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);

            if (!isPasswordValid)
            {
                throw new Exception("Email hoặc mật khẩu không đúng.");
            }

            return account.Employee;
        }

        public List<Employee> GetAllEmployees()
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            return _unitOfWork.Employees.AsQueryable().ToList();
        }

        public Employee GetEmployeeById(int id)
        {
            if (UserContext.CurrentUser != null && UserContext.CurrentUser.Role != EmployeeRole.Manager && UserContext.CurrentUser.Id != id)
            {
                throw new UnauthorizedAccessException("Chỉ có thể xem hồ sơ của chính mình hoặc bạn phải là Quản lý.");
            }
            return _unitOfWork.Employees.GetById(id);
        }

        private void ValidateEmployee(Employee employee, bool isNew)
        {
            // Tên: tối thiểu 2 từ, không số/ký tự đặc biệt
            if (string.IsNullOrWhiteSpace(employee.FullName))
                throw new ArgumentException("Họ và tên là bắt buộc.");
            var nameParts = employee.FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
                throw new ArgumentException("Họ và tên phải có tối thiểu 2 từ.");
            if (!Regex.IsMatch(employee.FullName, @"^[\p{L}\s]+$"))
                throw new ArgumentException("Họ và tên không được chứa số hoặc ký tự đặc biệt.");

            // Ngày sinh: đủ 18 tuổi
            var today = DateTime.Today;
            var age = today.Year - employee.DateOfBirth.Year;
            if (employee.DateOfBirth.Date > today.AddYears(-age)) age--;
            if (age < 18)
                throw new ArgumentException("Nhân viên phải từ 18 tuổi trở lên.");

            // SĐT: 10 số, bắt đầu bằng 0
            if (string.IsNullOrWhiteSpace(employee.PhoneNumber) || !Regex.IsMatch(employee.PhoneNumber, @"^0\d{9}$"))
                throw new ArgumentException("Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");

            // Email format
            if (string.IsNullOrWhiteSpace(employee.Email) || !Regex.IsMatch(employee.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Email không đúng định dạng.");

            // Unique Phone and Email
            var query = _unitOfWork.Employees.AsQueryable();
            if (!isNew) query = query.Where(e => e.Id != employee.Id);

            if (query.Any(e => e.PhoneNumber == employee.PhoneNumber))
                throw new ArgumentException("Số điện thoại này đã được sử dụng bởi nhân viên khác.");

            if (query.Any(e => e.Email == employee.Email))
                throw new ArgumentException("Email này đã được sử dụng bởi nhân viên khác.");

            // Image validation (simplistic check for DB level string of Url/Filename)
            if (!string.IsNullOrEmpty(employee.AvatarUrl))
            {
               var ext = System.IO.Path.GetExtension(employee.AvatarUrl).ToLower();
               if (ext != ".jpg" && ext != ".png" && ext != ".jpeg")
                   throw new ArgumentException("Ảnh đại diện chỉ chấp nhận định dạng jpg hoặc png.");
               if (System.IO.File.Exists(employee.AvatarUrl) && new System.IO.FileInfo(employee.AvatarUrl).Length > 5 * 1024 * 1024)
                   throw new ArgumentException("Ảnh đại diện không được vượt quá 5MB.");
            }

            // Role vs Degree
            if (employee.Role == EmployeeRole.Receptionist)
            {
                employee.Degree = null;
            }
            else if (employee.Role == EmployeeRole.Dentist && !employee.Degree.HasValue)
            {
                throw new ArgumentException("Trường Học vị là bắt buộc đối với Nha sĩ.");
            }
        }

        private string GenerateDefaultPassword(DateTime dob)
        {
            // ddmmyyyyAa@
            return string.Format("{0:ddMMyyyy}Aa@", dob);
        }

        private void ValidatePasswordFormat(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 32)
                throw new ArgumentException("Mật khẩu phải từ 8-32 ký tự.");

            if (!Regex.IsMatch(password, @"[A-Z]")) throw new ArgumentException("Mật khẩu phải có ít nhất 1 chữ hoa.");
            if (!Regex.IsMatch(password, @"[a-z]")) throw new ArgumentException("Mật khẩu phải có ít nhất 1 chữ thường.");
            if (!Regex.IsMatch(password, @"[0-9]")) throw new ArgumentException("Mật khẩu phải có ít nhất 1 số.");
            if (!Regex.IsMatch(password, @"[\W_]")) throw new ArgumentException("Mật khẩu phải có ít nhất 1 ký tự đặc biệt.");
        }

        private string GenerateResetToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
