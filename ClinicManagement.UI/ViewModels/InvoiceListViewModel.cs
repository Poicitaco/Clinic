using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
    public class InvoiceListViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private ObservableCollection<Invoice> _invoices;
        private Invoice _selectedInvoice;

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set
            {
                _invoices = value;
                OnPropertyChanged(nameof(Invoices));
            }
        }

        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged(nameof(SelectedInvoice));
                
                
            }
        }

        public ICommand AddCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand PayCommand { get; }
        public ICommand CancelCommand { get; }

        public InvoiceListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _invoiceService = new InvoiceService(unitOfWork);
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            ViewCommand = new RelayCommand(param => View(param));
            PayCommand = new RelayCommand(param => Pay(), param => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Pending);
            CancelCommand = new RelayCommand(param => Cancel(param), param => param is Invoice invoice && invoice.Status == InvoiceStatus.Pending);
        }

        private void LoadData()
        {
            using (var unitOfWork = new UnitOfWork(new ClinicDbContext()))
            {
                var invoiceService = new InvoiceService(unitOfWork);
                Invoices = new ObservableCollection<Invoice>(invoiceService.GetAllInvoices());
            }
        }

        private void Add()
        {
            var formViewModel = new InvoiceFormViewModel();
            var formWindow = new InvoiceFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void View(object parameter)
        {
            if (parameter is Invoice invoice)
            {
                var formViewModel = new InvoiceFormViewModel(invoice);
                formViewModel.PaymentChanged += LoadData;
                var formWindow = new InvoiceFormWindow { DataContext = formViewModel };
                formWindow.ShowDialog();
                LoadData();
            }
        }

        private void Pay()
        {
            if (SelectedInvoice == null) return;

            var result = MessageBox.Show("Xác nhận khách hàng đã thanh toán hóa đơn này?", 
                "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _invoiceService.PayInvoice(SelectedInvoice.Id);
                    LoadData();
                    MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel(object parameter)
        {
            if (!(parameter is Invoice invoice)) return;

            var reason = PromptCancelReason();
            if (string.IsNullOrWhiteSpace(reason)) return;

            try
            {
                _invoiceService.CancelInvoice(invoice.Id, reason);
                LoadData();
                MessageBox.Show("Hủy hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string PromptCancelReason()
        {
            var reasonBox = new TextBox { MinWidth = 300, Margin = new Thickness(0, 8, 0, 12) };
            var okButton = new Button { Content = "OK", Width = 80, IsDefault = true };
            var cancelButton = new Button { Content = "HỦY", Width = 80, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Nhập lý do hủy hóa đơn:" });
            panel.Children.Add(reasonBox);
            panel.Children.Add(buttons);

            var window = new Window
            {
                Title = "Hủy hóa đơn",
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            okButton.Click += (s, e) => window.DialogResult = true;

            return window.ShowDialog() == true ? reasonBox.Text : string.Empty;
        }
    }
}
