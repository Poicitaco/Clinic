using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ClinicManagement.Core;
using ClinicManagement.Business;

namespace ClinicManagement.UI.ViewModels
{
    public class EmployeeFormViewModel : ViewModelBase, IDataErrorInfo
    {
        private Employee _employee;
        private bool _isEditMode;

        private readonly IEmployeeService _service;
        public ICommand SaveCommand { get; }
        public event Action OnSaved;
        public event Action RequestClose;

        public EmployeeFormViewModel()
        {
            _employee = new Employee
            {
                StartDate = DateTime.Now,
                DateOfBirth = DateTime.Now.AddYears(-20),
                Role = EmployeeRole.Dentist,
                ContractStatus = ContractStatus.Working
            };
        }

        public EmployeeFormViewModel(IEmployeeService service) : this()
        {
            _service = service;
            SaveCommand = new ClinicManagement.UI.Utilities.RelayCommand(_ => Save(), _ => IsValid);
        }

        private void Save()
        {
            try
            {
                if (_isEditMode) _service.UpdateEmployee(_employee);
                else _service.CreateEmployee(_employee);
                
                OnSaved?.Invoke();
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                // In WPF, usually show a MessageBox or set an Error property
                System.Windows.MessageBox.Show(ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void LoadEmployee(Employee employee)
        {
            if (employee == null) return;

            _employee = new Employee
            {
                Id = employee.Id,
                FullName = employee.FullName,
                DateOfBirth = employee.DateOfBirth,
                Gender = employee.Gender,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
                AvatarUrl = employee.AvatarUrl,
                Role = employee.Role,
                StartDate = employee.StartDate,
                ContractStatus = employee.ContractStatus,
                Degree = employee.Degree,
                ResignationDate = employee.ResignationDate
            };
            _isEditMode = true;
            OnPropertyChanged(string.Empty);
        }

        public Employee GetEmployee() => _employee;

        public bool IsEditMode => _isEditMode;

        public string FullName
        {
            get => _employee.FullName;
            set { _employee.FullName = value; OnPropertyChanged(); }
        }

        public DateTime DateOfBirth
        {
            get => _employee.DateOfBirth;
            set { _employee.DateOfBirth = value; OnPropertyChanged(); }
        }

        public Gender Gender
        {
            get => _employee.Gender;
            set { _employee.Gender = value; OnPropertyChanged(); }
        }

        public string PhoneNumber
        {
            get => _employee.PhoneNumber;
            set { _employee.PhoneNumber = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _employee.Email;
            set { _employee.Email = value; OnPropertyChanged(); }
        }

        public string Address
        {
            get => _employee.Address;
            set { _employee.Address = value; OnPropertyChanged(); }
        }

        public string AvatarUrl
        {
            get => _employee.AvatarUrl;
            set { _employee.AvatarUrl = value; OnPropertyChanged(); }
        }

        public EmployeeRole Role
        {
            get => _employee.Role;
            set 
            { 
                _employee.Role = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsDegreeEnabled));
                if (!IsDegreeEnabled)
                {
                    Degree = AcademicDegree.None;
                }
            }
        }

        public DateTime StartDate
        {
            get => _employee.StartDate;
            set { _employee.StartDate = value; OnPropertyChanged(); }
        }

        public ContractStatus ContractStatus
        {
            get => _employee.ContractStatus;
            set { _employee.ContractStatus = value; OnPropertyChanged(); }
        }

        public AcademicDegree? Degree
        {
            get => _employee.Degree;
            set { _employee.Degree = value; OnPropertyChanged(); }
        }

        public bool IsDegreeEnabled => Role == EmployeeRole.Dentist;

        public Array Genders => Enum.GetValues(typeof(Gender));
        public Array Roles => Enum.GetValues(typeof(EmployeeRole));
        public Array ContractStatuses => Enum.GetValues(typeof(ContractStatus));
        public Array Degrees => Enum.GetValues(typeof(AcademicDegree));

        #region IDataErrorInfo

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string result = null;

                if (columnName == nameof(FullName))
                {
                    if (string.IsNullOrWhiteSpace(FullName))
                        result = "Họ và tên là bắt buộc.";
                    else if (FullName.Trim().Split(' ').Length < 2)
                        result = "Họ và tên tối thiểu 2 từ.";
                    else if (!Regex.IsMatch(FullName, @"^[\p{L}\s.]+$"))
                        result = "Họ và tên không được chứa số/ký tự đặc biệt.";
                }
                else if (columnName == nameof(DateOfBirth))
                {
                    var age = DateTime.Today.Year - DateOfBirth.Year;
                    if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                    if (age < 18)
                        result = "Nhân viên phải từ 18 tuổi trở lên.";
                }
                else if (columnName == nameof(PhoneNumber))
                {
                    if (string.IsNullOrWhiteSpace(PhoneNumber) || !Regex.IsMatch(PhoneNumber, @"^0\d{9}$"))
                        result = "SĐT phải gồm 10 chữ số và bắt đầu bằng số 0.";
                }
                else if (columnName == nameof(Email))
                {
                    if (string.IsNullOrWhiteSpace(Email) || !Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        result = "Email không đúng định dạng.";
                }
                else if (columnName == nameof(Degree))
                {
                    if (Role == EmployeeRole.Dentist && (!Degree.HasValue || Degree == AcademicDegree.None))
                        result = "Học vị là bắt buộc đối với Nha sĩ.";
                }

                return result;
            }
        }

        public bool IsValid
        {
            get
            {
                foreach (var prop in new[] { nameof(FullName), nameof(DateOfBirth), nameof(PhoneNumber), nameof(Email), nameof(Degree) })
                {
                    if (!string.IsNullOrEmpty(this[prop]))
                        return false;
                }
                return true;
            }
        }

        #endregion
    }
}
