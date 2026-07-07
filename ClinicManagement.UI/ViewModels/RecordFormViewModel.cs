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
    public class RecordFormViewModel : ViewModelBase
    {
        private readonly IPatientRecordService _recordService;
        private readonly bool _isEditMode;
        private readonly int _recordId;

        public ObservableCollection<Patient> Patients { get; set; }
        public ObservableCollection<Employee> Dentists { get; set; }
        public ObservableCollection<ExaminationStatus> Statuses { get; set; }

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(nameof(SelectedPatient)); }
        }

        private Employee _selectedDentist;
        public Employee SelectedDentist
        {
            get => _selectedDentist;
            set { _selectedDentist = value; OnPropertyChanged(nameof(SelectedDentist)); }
        }

        private DateTime _examinationDate;
        public DateTime ExaminationDate
        {
            get => _examinationDate;
            set { _examinationDate = value; OnPropertyChanged(nameof(ExaminationDate)); }
        }

        private string _symptoms;
        public string Symptoms
        {
            get => _symptoms;
            set { _symptoms = value; OnPropertyChanged(nameof(Symptoms)); }
        }

        private string _diagnosis;
        public string Diagnosis
        {
            get => _diagnosis;
            set { _diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); }
        }

        private string _treatmentPlan;
        public string TreatmentPlan
        {
            get => _treatmentPlan;
            set { _treatmentPlan = value; OnPropertyChanged(nameof(TreatmentPlan)); }
        }

        private string _prescription;
        public string Prescription
        {
            get => _prescription;
            set { _prescription = value; OnPropertyChanged(nameof(Prescription)); }
        }

        private string _proposedServices;
        public string ProposedServices
        {
            get => _proposedServices;
            set { _proposedServices = value; OnPropertyChanged(nameof(ProposedServices)); }
        }

        private string _dentalChartDetails;
        public string DentalChartDetails
        {
            get => _dentalChartDetails;
            set { _dentalChartDetails = value; OnPropertyChanged(nameof(DentalChartDetails)); }
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        private DateTime? _reExamDate;
        public DateTime? ReExamDate
        {
            get => _reExamDate;
            set { _reExamDate = value; OnPropertyChanged(nameof(ReExamDate)); }
        }

        private string _managerInterventionReason;
        public string ManagerInterventionReason
        {
            get => _managerInterventionReason;
            set { _managerInterventionReason = value; OnPropertyChanged(nameof(ManagerInterventionReason)); }
        }

        private ExaminationStatus _status;
        public ExaminationStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CanEdit));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsEditMode => _isEditMode;
        
        // Trạng thái cho phép sửa (chỉ khi chưa hoàn thành)
        public bool CanEdit => !_isEditMode || _status != ExaminationStatus.Finalized || UserContext.CurrentUser.Role == EmployeeRole.Manager;

        private string _error;
        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(nameof(Error)); }
        }

        public ICommand SaveCommand { get; }

        public RecordFormViewModel()
        {
            _isEditMode = false;
            ExaminationDate = DateTime.Now;
            Status = ExaminationStatus.Draft;

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _recordService = new PatientRecordService(unitOfWork);
            
            Patients = new ObservableCollection<Patient>(_recordService.GetAllPatients());
            Dentists = new ObservableCollection<Employee>(_recordService.GetAllDentists());
            Statuses = new ObservableCollection<ExaminationStatus>(Enum.GetValues(typeof(ExaminationStatus)).Cast<ExaminationStatus>());

            SelectedPatient = Patients.FirstOrDefault();
            SelectedDentist = Dentists.FirstOrDefault();

            SaveCommand = new RelayCommand(param => Save(param as Window), param => CanEdit);
        }

        public RecordFormViewModel(PatientExamination record)
        {
            _isEditMode = true;
            _recordId = record.Id;

            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _recordService = new PatientRecordService(unitOfWork);
            
            Patients = new ObservableCollection<Patient>(_recordService.GetAllPatients());
            Dentists = new ObservableCollection<Employee>(_recordService.GetAllDentists());
            Statuses = new ObservableCollection<ExaminationStatus>(Enum.GetValues(typeof(ExaminationStatus)).Cast<ExaminationStatus>());
            
            SelectedPatient = Patients.FirstOrDefault(p => p.Id == record.PatientId);
            SelectedDentist = Dentists.FirstOrDefault(d => d.Id == record.DentistId);
            ExaminationDate = record.ExaminationDate;
            Symptoms = record.Symptoms;
            Diagnosis = record.Diagnosis;
            TreatmentPlan = record.TreatmentPlan;
            Prescription = record.Prescription;
            ProposedServices = record.ProposedServices;
            DentalChartDetails = record.DentalChartDetails;
            Notes = record.Notes;
            ReExamDate = record.ReExamDate;
            ManagerInterventionReason = record.ManagerInterventionReason;
            Status = record.Status;

            SaveCommand = new RelayCommand(param => Save(param as Window), param => CanEdit);
        }

        private void Save(Window window)
        {
            try
            {
                if (SelectedPatient == null)
                    throw new Exception("Vui lòng chọn Bệnh nhân.");
                if (SelectedDentist == null)
                    throw new Exception("Vui lòng chọn Nha sĩ.");

                var record = new PatientExamination
                {
                    PatientId = SelectedPatient.Id,
                    DentistId = SelectedDentist.Id,
                    ExaminationDate = ExaminationDate,
                    Symptoms = Symptoms,
                    Diagnosis = Diagnosis,
                    TreatmentPlan = TreatmentPlan,
                    Prescription = Prescription,
                    ProposedServices = ProposedServices,
                    DentalChartDetails = DentalChartDetails,
                    Notes = Notes,
                    ReExamDate = ReExamDate,
                    ManagerInterventionReason = ManagerInterventionReason,
                    Status = Status
                };

                if (_isEditMode)
                {
                    record.Id = _recordId;
                    _recordService.UpdateRecord(record);
                }
                else
                {
                    _recordService.AddRecord(record);
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
