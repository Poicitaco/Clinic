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
using ClinicManagement.UI.Views;

namespace ClinicManagement.UI.ViewModels
{
    public class AppointmentListViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly UnitOfWork _unitOfWork;
        private ObservableCollection<Appointment> _appointments;
        private Appointment _selectedAppointment;

        public ObservableCollection<Appointment> Appointments
        {
            get => _appointments;
            set
            {
                _appointments = value;
                OnPropertyChanged(nameof(Appointments));
            }
        }

        public Appointment SelectedAppointment
        {
            get => _selectedAppointment;
            set
            {
                _selectedAppointment = value;
                OnPropertyChanged(nameof(SelectedAppointment));
                
                
            }
        }

        public ObservableCollection<Employee> Dentists { get; private set; }
        public ObservableCollection<Employee> Creators { get; private set; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); LoadData(); }
        }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set { _selectedDate = value; OnPropertyChanged(nameof(SelectedDate)); LoadData(); }
        }

        private Employee _selectedDentist;
        public Employee SelectedDentist
        {
            get => _selectedDentist;
            set { _selectedDentist = value; OnPropertyChanged(nameof(SelectedDentist)); LoadData(); }
        }

        private Employee _selectedCreator;
        public Employee SelectedCreator
        {
            get => _selectedCreator;
            set { _selectedCreator = value; OnPropertyChanged(nameof(SelectedCreator)); LoadData(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand CancelAppCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public AppointmentListViewModel()
        {
            _unitOfWork = new UnitOfWork(new ClinicDbContext());
            _appointmentService = new AppointmentService(_unitOfWork);

            Dentists = new ObservableCollection<Employee>(_unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Dentist).OrderBy(e => e.FullName));
            Creators = new ObservableCollection<Employee>(_unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Manager || e.Role == EmployeeRole.Receptionist).OrderBy(e => e.FullName));
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(param as Appointment), param => param is Appointment || SelectedAppointment != null);
            CancelAppCommand = new RelayCommand(param => Cancel(param as Appointment), param => param is Appointment || SelectedAppointment != null);
            ClearFiltersCommand = new RelayCommand(param => ClearFilters());
        }

        private void LoadData()
        {
            var query = _appointmentService.GetAllAppointments().AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var keyword = SearchText.Trim().ToLowerInvariant();
                query = query.Where(a =>
                    (a.PatientName ?? string.Empty).ToLowerInvariant().Contains(keyword) ||
                    (a.PhoneNumber ?? string.Empty).Contains(keyword));
            }

            if (SelectedDate.HasValue)
                query = query.Where(a => a.StartTime.Date == SelectedDate.Value.Date);

            if (SelectedDentist != null)
                query = query.Where(a => a.DentistId == SelectedDentist.Id);

            if (SelectedCreator != null)
                query = query.Where(a => a.CreatedById == SelectedCreator.Id);

            Appointments = new ObservableCollection<Appointment>(
                query.OrderByDescending(a => a.StartTime).Take(100).ToList());
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedDate = null;
            SelectedDentist = null;
            SelectedCreator = null;
            LoadData();
        }

        private void Add()
        {
            var formViewModel = new AppointmentFormViewModel();
            var formWindow = new AppointmentFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Edit(Appointment appointment)
        {
            appointment = appointment ?? SelectedAppointment;
            if (appointment == null) return;
            var formViewModel = new AppointmentFormViewModel(appointment);
            var formWindow = new AppointmentFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Cancel(Appointment appointment)
        {
            appointment = appointment ?? SelectedAppointment;
            if (appointment == null) return;

            if (appointment.Status == AppointmentStatus.Completed)
            {
                MessageBox.Show("Không thể hủy lịch hẹn đã hoàn thành.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(string.Format("Bạn có chắc chắn muốn hủy lịch hẹn của bệnh nhân '{0}' không?", appointment.PatientName), 
                "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _appointmentService.CancelAppointment(appointment.Id);
                    LoadData();
                    MessageBox.Show("Hủy lịch hẹn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
