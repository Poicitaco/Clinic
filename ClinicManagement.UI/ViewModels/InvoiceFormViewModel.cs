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
    public class InvoiceFormViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        public event Action PaymentChanged;

        public ObservableCollection<PatientExamination> Examinations { get; set; }
        public ObservableCollection<Service> AvailableServices { get; set; }
        
        public ObservableCollection<InvoiceDetail> SelectedDetails { get; set; }

        private PatientExamination _selectedExamination;
        public PatientExamination SelectedExamination
        {
            get => _selectedExamination;
            set { _selectedExamination = value; OnPropertyChanged(nameof(SelectedExamination)); CommandManager.InvalidateRequerySuggested(); }
        }

        private Service _selectedServiceToAdd;
        public Service SelectedServiceToAdd
        {
            get => _selectedServiceToAdd;
            set { _selectedServiceToAdd = value; OnPropertyChanged(nameof(SelectedServiceToAdd)); CommandManager.InvalidateRequerySuggested(); }
        }

        private InvoiceDetail _selectedDetailToRemove;
        public InvoiceDetail SelectedDetailToRemove
        {
            get => _selectedDetailToRemove;
            set { _selectedDetailToRemove = value; OnPropertyChanged(nameof(SelectedDetailToRemove)); CommandManager.InvalidateRequerySuggested(); }
        }

        public decimal TotalAmount => SelectedDetails.Sum(d => d.UnitPrice * d.Quantity);

        private string _error;
        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(nameof(Error)); }
        }

        public bool IsReadOnly { get; set; }
        public bool IsCreating => !IsReadOnly;

        private int _invoiceId;

        private ObservableCollection<InvoicePayment> _payments;
        public ObservableCollection<InvoicePayment> Payments
        {
            get => _payments;
            set { _payments = value; OnPropertyChanged(nameof(Payments)); }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set { _paidAmount = value; OnPropertyChanged(nameof(PaidAmount)); OnPropertyChanged(nameof(RemainingAmount)); }
        }

        public decimal RemainingAmount => TotalAmount - PaidAmount;

        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set { _paymentAmount = value; OnPropertyChanged(nameof(PaymentAmount)); }
        }

        private string _paymentNote;
        public string PaymentNote
        {
            get => _paymentNote;
            set { _paymentNote = value; OnPropertyChanged(nameof(PaymentNote)); }
        }

        public ICommand AddServiceCommand { get; }
        public ICommand RemoveServiceCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddPaymentCommand { get; }

        // Mở Form để tạo mới Hóa đơn
        public InvoiceFormViewModel()
        {
            IsReadOnly = false;
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _invoiceService = new InvoiceService(unitOfWork);
            
            Examinations = new ObservableCollection<PatientExamination>(_invoiceService.GetCompletedExaminationsWithoutInvoice());
            AvailableServices = new ObservableCollection<Service>(_invoiceService.GetAllServices());
            SelectedDetails = new ObservableCollection<InvoiceDetail>();

            SelectedDetails.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
                CommandManager.InvalidateRequerySuggested();
            };

            AddServiceCommand = new RelayCommand(param => AddService(), param => SelectedServiceToAdd != null);
            RemoveServiceCommand = new RelayCommand(param => RemoveService(), param => SelectedDetailToRemove != null);
            SaveCommand = new RelayCommand(param => Save(param as Window), param => IsCreating && SelectedDetails.Any() && SelectedExamination != null);
        }

        // Mở Form chỉ để xem (sau khi hóa đơn đã được tạo)
        public InvoiceFormViewModel(Invoice invoice)
        {
            IsReadOnly = true;
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _invoiceService = new InvoiceService(unitOfWork);

            _invoiceId = invoice.Id;
            var details = unitOfWork.InvoiceDetails.Find(d => d.InvoiceId == invoice.Id).ToList();
            SelectedDetails = new ObservableCollection<InvoiceDetail>(details);
            
            Examinations = new ObservableCollection<PatientExamination> { invoice.Examination };
            SelectedExamination = invoice.Examination;
            AvailableServices = new ObservableCollection<Service>();

            AddServiceCommand = new RelayCommand(param => { });
            RemoveServiceCommand = new RelayCommand(param => { });
            SaveCommand = new RelayCommand(param => { });

            LoadPayments(unitOfWork);
            AddPaymentCommand = new RelayCommand(param => AddPayment(), param => IsReadOnly && RemainingAmount > 0);
        }

        private void LoadPayments(UnitOfWork unitOfWork)
        {
            var invoice = unitOfWork.Invoices.GetById(_invoiceId);
            if (invoice != null)
            {
                var paymentsList = unitOfWork.InvoicePayments.Find(p => p.InvoiceId == _invoiceId).OrderByDescending(p => p.PaymentDate).ToList();
                Payments = new ObservableCollection<InvoicePayment>(paymentsList);
                PaidAmount = invoice.PaidAmount;
                OnPropertyChanged(nameof(RemainingAmount));
            }
        }

        private void AddPayment()
        {
            if (PaymentAmount <= 0)
            {
                MessageBox.Show("Số tiền thanh toán phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (PaymentAmount > RemainingAmount)
            {
                MessageBox.Show("Số tiền đóng vượt quá số tiền còn nợ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _invoiceService.AddPayment(_invoiceId, PaymentAmount, PaymentNote, UserContext.CurrentUser.Id);
                
                var unitOfWork = new UnitOfWork(new ClinicDbContext());
                LoadPayments(unitOfWork);
                PaymentAmount = 0;
                PaymentNote = string.Empty;
                PaymentChanged?.Invoke();
                CommandManager.InvalidateRequerySuggested();
                
                MessageBox.Show("Thêm đợt thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddService()
        {
            if (SelectedServiceToAdd == null) return;
            
            var existingDetail = SelectedDetails.FirstOrDefault(d => d.ServiceId == SelectedServiceToAdd.Id);
            if (existingDetail != null)
            {
                existingDetail.Quantity++;
                // Trigger refresh if needed, but simple re-assignment forces update
                var temp = SelectedDetails.ToList();
                SelectedDetails.Clear();
                foreach(var item in temp) SelectedDetails.Add(item);
            }
            else
            {
                SelectedDetails.Add(new InvoiceDetail 
                { 
                    ServiceId = SelectedServiceToAdd.Id, 
                    Service = SelectedServiceToAdd,
                    Quantity = 1, 
                    UnitPrice = SelectedServiceToAdd.Price 
                });
            }
        }

        private void RemoveService()
        {
            if (SelectedDetailToRemove != null)
            {
                SelectedDetails.Remove(SelectedDetailToRemove);
            }
        }

        private void Save(Window window)
        {
            try
            {
                if (SelectedExamination == null)
                    throw new Exception("Vui lòng chọn bệnh án để xuất hóa đơn.");

                var invoice = new Invoice
                {
                    ExaminationId = SelectedExamination.Id,
                    CreatedDate = DateTime.Now,
                    Status = InvoiceStatus.Pending,
                    TotalAmount = TotalAmount // will be recalculated in Service anyway
                };

                _invoiceService.AddInvoice(invoice, SelectedDetails.ToList());

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
