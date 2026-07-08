using System;
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

namespace ClinicManagement.UI.ViewModels
{
    public class PatientListViewModel : ViewModelBase
    {
        private readonly IPatientRecordService _patientService;
        private ObservableCollection<Patient> _patients;
        private string _searchText;

        public ObservableCollection<Patient> Patients
        {
            get => _patients;
            set { _patients = value; OnPropertyChanged(nameof(Patients)); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); LoadData(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }

        public PatientListViewModel()
        {
            _patientService = new PatientRecordService(new UnitOfWork(new ClinicDbContext()));
            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(p => Edit(p as Patient), p => p is Patient);
            LoadData();
        }

        private void LoadData()
        {
            var query = _patientService.GetAllPatients().AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var keyword = SearchText.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    (p.FullName ?? string.Empty).ToLowerInvariant().Contains(keyword) ||
                    (p.PhoneNumber ?? string.Empty).Contains(keyword));
            }

            Patients = new ObservableCollection<Patient>(query.OrderBy(p => p.FullName).ToList());
        }

        private void Add()
        {
            var patient = PromptPatient("Thêm bệnh nhân", null);
            if (patient == null) return;

            try
            {
                _patientService.AddPatient(patient);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit(Patient patient)
        {
            if (patient == null) return;
            var edited = PromptPatient("Sửa bệnh nhân", patient);
            if (edited == null) return;

            try
            {
                _patientService.UpdatePatient(edited);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Patient PromptPatient(string title, Patient seed)
        {
            var nameBox = new TextBox { Text = seed?.FullName ?? string.Empty, MinWidth = 340, Margin = new Thickness(0, 4, 0, 8) };
            var phoneBox = new TextBox { Text = seed?.PhoneNumber ?? string.Empty, Margin = new Thickness(0, 4, 0, 8) };
            var emailBox = new TextBox { Text = seed?.Email ?? string.Empty, Margin = new Thickness(0, 4, 0, 8) };
            var addressBox = new TextBox { Text = seed?.Address ?? string.Empty, Margin = new Thickness(0, 4, 0, 8) };
            var dobPicker = new DatePicker { SelectedDate = seed?.DateOfBirth ?? DateTime.Today.AddYears(-20), Margin = new Thickness(0, 4, 0, 8) };
            var genderBox = new ComboBox { ItemsSource = Enum.GetValues(typeof(Gender)), SelectedItem = seed?.Gender ?? Gender.Male, Margin = new Thickness(0, 4, 0, 12) };

            var okButton = new Button { Content = "LƯU", Width = 90, IsDefault = true };
            var cancelButton = new Button { Content = "HỦY", Width = 90, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Họ tên" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new TextBlock { Text = "Số điện thoại" });
            panel.Children.Add(phoneBox);
            panel.Children.Add(new TextBlock { Text = "Ngày sinh" });
            panel.Children.Add(dobPicker);
            panel.Children.Add(new TextBlock { Text = "Giới tính" });
            panel.Children.Add(genderBox);
            panel.Children.Add(new TextBlock { Text = "Email" });
            panel.Children.Add(emailBox);
            panel.Children.Add(new TextBlock { Text = "Địa chỉ" });
            panel.Children.Add(addressBox);
            panel.Children.Add(buttons);

            var window = new Window { Title = title, Content = panel, SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            okButton.Click += (s, e) => window.DialogResult = true;
            if (window.ShowDialog() != true) return null;

            return new Patient
            {
                Id = seed?.Id ?? 0,
                FullName = nameBox.Text,
                PhoneNumber = phoneBox.Text,
                DateOfBirth = dobPicker.SelectedDate ?? DateTime.Today.AddYears(-20),
                Gender = (Gender)genderBox.SelectedItem,
                Email = emailBox.Text,
                Address = addressBox.Text
            };
        }
    }
}
