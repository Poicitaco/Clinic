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
    public class ServiceFormViewModel : ViewModelBase
    {
        private readonly IServiceService _serviceService;
        private readonly bool _isEditMode;
        private readonly int _serviceId;

        public ObservableCollection<ServiceCategory> Categories { get; set; }
        public ObservableCollection<ServiceStage> Stages { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private ServiceCategory _selectedCategory;
        public ServiceCategory SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        private bool _isMultiStage;
        public bool IsMultiStage
        {
            get => _isMultiStage;
            set 
            { 
                _isMultiStage = value; 
                OnPropertyChanged(nameof(IsMultiStage)); 
                if (!_isMultiStage)
                {
                    Stages.Clear();
                }
                else if (Stages.Count == 0)
                {
                    Stages.Add(new ServiceStage { Name = "Giai đoạn 1", Percentage = 100, Order = 1 });
                }
            }
        }

        private string _error;
        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(nameof(Error)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand AddStageCommand { get; }
        public ICommand RemoveStageCommand { get; }

        // Default constructor for Adding
        public ServiceFormViewModel()
        {
            _isEditMode = false;
            IsActive = true;
            Stages = new ObservableCollection<ServiceStage>();

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _serviceService = new ServiceService(unitOfWork);

            Categories = new ObservableCollection<ServiceCategory>(_serviceService.GetAllCategories());
            SelectedCategory = Categories.FirstOrDefault();

            SaveCommand = new RelayCommand(param => Save(param as Window));
            AddStageCommand = new RelayCommand(param => AddStage());
            RemoveStageCommand = new RelayCommand(param => RemoveStage(param as ServiceStage));
        }

        // Constructor for Editing
        public ServiceFormViewModel(Service service)
        {
            _isEditMode = true;
            _serviceId = service.Id;
            Stages = new ObservableCollection<ServiceStage>();

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _serviceService = new ServiceService(unitOfWork);

            Categories = new ObservableCollection<ServiceCategory>(_serviceService.GetAllCategories());
            
            Name = service.Name;
            Description = service.Description;
            IsActive = service.IsActive;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == service.CategoryId);
            
            // Lấy lại _serviceService db để query stage. 
            // Better to load Stages with Service from DB, but we pass full service here.
            IsMultiStage = service.IsMultiStage;
            if (service.Stages != null)
            {
                foreach(var s in service.Stages)
                {
                    Stages.Add(new ServiceStage { Id = s.Id, Name = s.Name, Percentage = s.Percentage, Order = s.Order });
                }
            }

            SaveCommand = new RelayCommand(param => Save(param as Window));
            AddStageCommand = new RelayCommand(param => AddStage());
            RemoveStageCommand = new RelayCommand(param => RemoveStage(param as ServiceStage));
        }

        private void AddStage()
        {
            int nextOrder = Stages.Count > 0 ? Stages.Max(s => s.Order) + 1 : 1;
            Stages.Add(new ServiceStage { Name = $"Giai đoạn {nextOrder}", Percentage = 0, Order = nextOrder });
        }

        private void RemoveStage(ServiceStage stage)
        {
            if (stage != null)
            {
                Stages.Remove(stage);
            }
        }

        private void Save(Window window)
        {
            try
            {
                if (SelectedCategory == null)
                    throw new Exception("Vui lòng chọn danh mục dịch vụ.");

                var service = new Service
                {
                    Name = Name,
                    Description = Description,
                    IsActive = IsActive,
                    CategoryId = SelectedCategory.Id,
                    IsMultiStage = IsMultiStage
                };

                if (IsMultiStage)
                {
                    foreach(var s in Stages)
                    {
                        service.Stages.Add(s);
                    }
                }

                if (_isEditMode)
                {
                    service.Id = _serviceId;
                    _serviceService.UpdateService(service);
                }
                else
                {
                    _serviceService.AddService(service);
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
