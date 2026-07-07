using System;
using System.Collections.ObjectModel;
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
    public class ServicePriceHistoryViewModel : ViewModelBase
    {
        private readonly IServiceService _serviceService;
        private readonly int _serviceId;

        public Service CurrentService { get; set; }

        private ObservableCollection<ServicePriceHistory> _priceHistory;
        public ObservableCollection<ServicePriceHistory> PriceHistory
        {
            get => _priceHistory;
            set { _priceHistory = value; OnPropertyChanged(nameof(PriceHistory)); }
        }

        private decimal _newPrice;
        public decimal NewPrice
        {
            get => _newPrice;
            set { _newPrice = value; OnPropertyChanged(nameof(NewPrice)); }
        }

        private DateTime _newEffectiveDate = DateTime.Today.AddDays(1);
        public DateTime NewEffectiveDate
        {
            get => _newEffectiveDate;
            set { _newEffectiveDate = value; OnPropertyChanged(nameof(NewEffectiveDate)); }
        }

        private string _error;
        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(nameof(Error)); }
        }

        public ICommand SetPriceCommand { get; }

        public ServicePriceHistoryViewModel(Service service)
        {
            _serviceId = service.Id;
            CurrentService = service;
            NewPrice = service.Price; // Gợi ý giá hiện tại

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _serviceService = new ServiceService(unitOfWork);

            LoadHistory();

            SetPriceCommand = new RelayCommand(param => SetPrice());
        }

        private void LoadHistory()
        {
            var history = _serviceService.GetServicePriceHistory(_serviceId);
            PriceHistory = new ObservableCollection<ServicePriceHistory>(history);
        }

        private void SetPrice()
        {
            try
            {
                Error = string.Empty;
                
                if (NewEffectiveDate.Date <= DateTime.Now.Date)
                {
                    // Cho phép đặt từ hôm nay hoặc ngày mai, nếu <= today thì ok nhưng phải lớn hơn thời điểm đã chốt lịch sử gần nhất nếu muốn chặt chẽ.
                    // Theo SRS: Ngày có hiệu lực phải sau ngày hiện tại ít nhất 1 ngày. Nhưng để linh hoạt có thể cho phép set giá mới ngay hôm nay.
                    // SRS "Ngày có hiệu lực phải sau ngày hiện tại ít nhất 1 ngày."
                    if (NewEffectiveDate.Date <= DateTime.Now.Date)
                        throw new Exception("Ngày có hiệu lực phải sau ngày hiện tại ít nhất 1 ngày.");
                }

                _serviceService.SetServicePrice(_serviceId, NewPrice, NewEffectiveDate);
                
                MessageBox.Show("Thiết lập giá mới thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadHistory();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
