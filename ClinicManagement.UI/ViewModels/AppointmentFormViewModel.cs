using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Business.Services;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class AppointmentFormViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly bool _isEditMode;
        private readonly int _appointmentId;

        public ObservableCollection<Employee> Dentists { get; set; }
        public ObservableCollection<AppointmentStatus> Statuses { get; set; }

        private string _patientName;
        public string PatientName
        {
            get => _patientName;
            set { _patientName = value; OnPropertyChanged(nameof(PatientName)); }
        }

        private string _phoneNumber;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(nameof(PhoneNumber)); }
        }

        private Employee _selectedDentist;
        public Employee SelectedDentist
        {
            get => _selectedDentist;
            set { _selectedDentist = value; OnPropertyChanged(nameof(SelectedDentist)); }
        }

        private DateTime? _appointmentDate;
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set { _appointmentDate = value; OnPropertyChanged(nameof(AppointmentDate)); }
        }

        private DateTime? _appointmentTime;
        public DateTime? AppointmentTime
        {
            get => _appointmentTime;
            set { _appointmentTime = value; OnPropertyChanged(nameof(AppointmentTime)); }
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        private AppointmentStatus _status;
        public AppointmentStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public bool IsEditMode => _isEditMode;

        private string _error;
        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(nameof(Error)); }
        }

        public ICommand SaveCommand { get; }

        public AppointmentFormViewModel()
        {
            _isEditMode = false;
            AppointmentDate = DateTime.Today.AddDays(1);
            AppointmentTime = DateTime.Today.Add(new TimeSpan(8, 0, 0));
            Status = AppointmentStatus.Pending;

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _appointmentService = new AppointmentService(unitOfWork);
            
            Dentists = new ObservableCollection<Employee>(unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Dentist && e.ContractStatus == ContractStatus.Working));
            SelectedDentist = Dentists.FirstOrDefault();
            
            Statuses = new ObservableCollection<AppointmentStatus>(Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>());

            SaveCommand = new RelayCommand(param => Save(param as Window));
        }

        public AppointmentFormViewModel(Appointment appointment)
        {
            _isEditMode = true;
            _appointmentId = appointment.Id;

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _appointmentService = new AppointmentService(unitOfWork);
            
            Dentists = new ObservableCollection<Employee>(unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Dentist && e.ContractStatus == ContractStatus.Working));
            Statuses = new ObservableCollection<AppointmentStatus>(Enum.GetValues(typeof(AppointmentStatus)).Cast<AppointmentStatus>());
            
            PatientName = appointment.PatientName;
            PhoneNumber = appointment.PhoneNumber;
            SelectedDentist = Dentists.FirstOrDefault(d => d.Id == appointment.DentistId);
            AppointmentDate = appointment.StartTime.Date;
            AppointmentTime = DateTime.Today.Add(appointment.StartTime.TimeOfDay);
            Status = appointment.Status;

            SaveCommand = new RelayCommand(param => Save(param as Window));
        }

        private void Save(Window window)
        {
            try
            {
                if (SelectedDentist == null)
                    throw new Exception("Vui lòng chọn Nha sĩ phụ trách.");

                if (!AppointmentDate.HasValue)
                    throw new Exception("Vui lòng chọn ngày hẹn.");
                if (!AppointmentTime.HasValue)
                    throw new Exception("Vui lòng chọn giờ hẹn.");

                var startTime = AppointmentDate.Value.Date.Add(AppointmentTime.Value.TimeOfDay);
                var endTime = startTime.AddHours(1); // Mặc định mỗi ca hẹn 1 tiếng

                var appointment = new Appointment
                {
                    PatientName = PatientName,
                    PhoneNumber = PhoneNumber,
                    DentistId = SelectedDentist.Id,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = Status
                };

                if (_isEditMode)
                {
                    appointment.Id = _appointmentId;
                    _appointmentService.UpdateAppointment(appointment);
                }
                else
                {
                    _appointmentService.AddAppointment(appointment);
                }

                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
