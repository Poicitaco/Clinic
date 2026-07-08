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
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }

        public ServiceListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _serviceService = new ServiceService(unitOfWork);
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(param as Service), param => param is Service || SelectedService != null);
            DeleteCommand = new RelayCommand(param => Delete(param as Service), param => param is Service || SelectedService != null);
            PriceHistoryCommand = new RelayCommand(param => OpenPriceHistory(param as Service), param => param is Service || SelectedService != null);
            AddCategoryCommand = new RelayCommand(param => AddCategory());
            EditCategoryCommand = new RelayCommand(param => EditCategory());
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

        private void AddCategory()
        {
            var category = PromptCategory("Thêm nhóm dịch vụ", null);
            if (category == null) return;

            try
            {
                _serviceService.AddCategory(category);
                MessageBox.Show("Thêm nhóm dịch vụ thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategory()
        {
            var categories = _serviceService.GetAllCategories().ToList();
            if (!categories.Any())
            {
                MessageBox.Show("Chưa có nhóm dịch vụ để sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selected = categories.First();
            var combo = new ComboBox { ItemsSource = categories, DisplayMemberPath = "Name", SelectedItem = selected, Margin = new Thickness(0, 8, 0, 8), MinWidth = 320 };
            var nameBox = new TextBox { Text = selected.Name, Margin = new Thickness(0, 0, 0, 8) };
            var descriptionBox = new TextBox { Text = selected.Description, Margin = new Thickness(0, 0, 0, 12) };
            combo.SelectionChanged += (s, e) =>
            {
                selected = combo.SelectedItem as ServiceCategory;
                if (selected == null) return;
                nameBox.Text = selected.Name;
                descriptionBox.Text = selected.Description;
            };

            var edited = PromptCategory("Sửa nhóm dịch vụ", selected, combo, nameBox, descriptionBox);
            if (edited == null) return;

            try
            {
                _serviceService.UpdateCategory(edited);
                MessageBox.Show("Cập nhật nhóm dịch vụ thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static ServiceCategory PromptCategory(string title, ServiceCategory seed, ComboBox combo = null, TextBox nameBox = null, TextBox descriptionBox = null)
        {
            nameBox = nameBox ?? new TextBox { Text = seed?.Name ?? string.Empty, Margin = new Thickness(0, 8, 0, 8), MinWidth = 320 };
            descriptionBox = descriptionBox ?? new TextBox { Text = seed?.Description ?? string.Empty, Margin = new Thickness(0, 0, 0, 12), MinWidth = 320 };

            var okButton = new Button { Content = "LƯU", Width = 90, IsDefault = true };
            var cancelButton = new Button { Content = "HỦY", Width = 90, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel { Margin = new Thickness(16) };
            if (combo != null)
            {
                panel.Children.Add(new TextBlock { Text = "Chọn nhóm" });
                panel.Children.Add(combo);
            }
            panel.Children.Add(new TextBlock { Text = "Tên nhóm" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new TextBlock { Text = "Mô tả" });
            panel.Children.Add(descriptionBox);
            panel.Children.Add(buttons);

            var window = new Window
            {
                Title = title,
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            okButton.Click += (s, e) => window.DialogResult = true;

            if (window.ShowDialog() != true)
                return null;

            var category = combo?.SelectedItem as ServiceCategory ?? seed ?? new ServiceCategory();
            return new ServiceCategory
            {
                Id = category.Id,
                Name = nameBox.Text,
                Description = descriptionBox.Text
            };
        }
    }
}
