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

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand CancelAppCommand { get; }

        public AppointmentListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _appointmentService = new AppointmentService(unitOfWork);
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(param as Appointment), param => param is Appointment || SelectedAppointment != null);
            CancelAppCommand = new RelayCommand(param => Cancel(param as Appointment), param => param is Appointment || SelectedAppointment != null);
        }

        private void LoadData()
        {
            Appointments = new ObservableCollection<Appointment>(_appointmentService.GetAllAppointments());
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
