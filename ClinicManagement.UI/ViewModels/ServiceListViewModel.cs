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
    public class ServiceListViewModel : ViewModelBase
    {
        private readonly IServiceService _serviceService;
        private ObservableCollection<Service> _services;
        private Service _selectedService;

        public ObservableCollection<Service> Services
        {
            get => _services;
            set
            {
                _services = value;
                OnPropertyChanged(nameof(Services));
            }
        }

        public Service SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged(nameof(SelectedService));
                
                
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PriceHistoryCommand { get; }

        public ServiceListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _serviceService = new ServiceService(unitOfWork);
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(param as Service), param => param is Service || SelectedService != null);
            DeleteCommand = new RelayCommand(param => Delete(param as Service), param => param is Service || SelectedService != null);
            PriceHistoryCommand = new RelayCommand(param => OpenPriceHistory(param as Service), param => param is Service || SelectedService != null);
        }

        private void LoadData()
        {
            Services = new ObservableCollection<Service>(_serviceService.GetAllServices());
        }

        private void Add()
        {
            var formViewModel = new ServiceFormViewModel();
            var formWindow = new ServiceFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Edit(Service service)
        {
            service = service ?? SelectedService;
            if (service == null) return;
            var formViewModel = new ServiceFormViewModel(service);
            var formWindow = new ServiceFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Delete(Service service)
        {
            service = service ?? SelectedService;
            if (service == null) return;

            var result = MessageBox.Show(string.Format("Bạn có chắc chắn muốn xóa dịch vụ '{0}' không?", service.Name), 
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _serviceService.DeleteService(service.Id);
                    LoadData();
                    MessageBox.Show("Xóa dịch vụ thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPriceHistory(Service service)
        {
            service = service ?? SelectedService;
            if (service == null) return;
            var viewModel = new ServicePriceHistoryViewModel(service);
            var window = new ServicePriceHistoryWindow { DataContext = viewModel };
            window.ShowDialog();
            LoadData(); // reload in case price changed
        }
    }
}
