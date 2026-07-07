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
    public class SalaryConfigViewModel : ViewModelBase
    {
        private readonly ISalaryService _salaryService;

        private SalaryConfiguration _configuration;
        public SalaryConfiguration Configuration
        {
            get => _configuration;
            set => SetProperty(ref _configuration, value);
        }

        public ObservableCollection<DentistDegreeSalaryCoefficient> DentistDegreeCoefficients { get; set; }
        public ICommand SaveCommand { get; }

        public SalaryConfigViewModel()
        {
            var context = new ClinicDbContext();
            var unitOfWork = new UnitOfWork(context);
            _salaryService = new SalaryService(unitOfWork);

            DentistDegreeCoefficients = new ObservableCollection<DentistDegreeSalaryCoefficient>();
            SaveCommand = new RelayCommand(param => SaveConfigs());

            LoadConfigs();
        }

        private void LoadConfigs()
        {
            Configuration = _salaryService.GetAllConfigurations().FirstOrDefault()
                ?? new SalaryConfiguration
                {
                    HourlyRate = 1000m,
                    DefaultShiftCoefficient = 1.0m,
                    ReceptionistCoefficient = 1.0m
                };

            var existingCoefficients = Configuration.DentistDegreeCoefficients?.ToList()
                ?? new System.Collections.Generic.List<DentistDegreeSalaryCoefficient>();

            foreach (var degree in new[]
            {
                AcademicDegree.Doctor,
                AcademicDegree.Master,
                AcademicDegree.PhD,
                AcademicDegree.AssociateProfessor,
                AcademicDegree.Professor
            })
            {
                DentistDegreeCoefficients.Add(existingCoefficients.FirstOrDefault(c => c.Degree == degree)
                    ?? new DentistDegreeSalaryCoefficient { Degree = degree, Coefficient = 1.0m });
            }

            Configuration.DentistDegreeCoefficients = DentistDegreeCoefficients;
        }

        private void SaveConfigs()
        {
            try
            {
                Configuration.DentistDegreeCoefficients = DentistDegreeCoefficients;
                _salaryService.UpdateConfiguration(Configuration);
                MessageBox.Show("Lưu cấu hình lương thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
