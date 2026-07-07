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
    public class RecordListViewModel : ViewModelBase
    {
        private readonly IPatientRecordService _recordService;
        private ObservableCollection<PatientExamination> _records;
        private PatientExamination _selectedRecord;

        public ObservableCollection<PatientExamination> Records
        {
            get => _records;
            set
            {
                _records = value;
                OnPropertyChanged(nameof(Records));
            }
        }

        public PatientExamination SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                _selectedRecord = value;
                OnPropertyChanged(nameof(SelectedRecord));
                
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }

        public RecordListViewModel()
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _recordService = new PatientRecordService(unitOfWork);
            
            LoadData();

            AddCommand = new RelayCommand(param => Add());
            EditCommand = new RelayCommand(param => Edit(param as PatientExamination), param => param is PatientExamination || SelectedRecord != null);
        }

        private void LoadData()
        {
            Records = new ObservableCollection<PatientExamination>(_recordService.GetAllRecords());
        }

        private void Add()
        {
            var formViewModel = new RecordFormViewModel();
            var formWindow = new RecordFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void Edit(PatientExamination record)
        {
            record = record ?? SelectedRecord;
            if (record == null) return;
            var formViewModel = new RecordFormViewModel(record);
            var formWindow = new RecordFormWindow { DataContext = formViewModel };
            if (formWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }
    }
}
