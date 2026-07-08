using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Interfaces
{
    public interface IPatientRecordService
    {
        IEnumerable<PatientExamination> GetAllRecords();
        PatientExamination GetRecordById(int id);
        void AddRecord(PatientExamination record);
        void UpdateRecord(PatientExamination record);
        IEnumerable<Patient> GetAllPatients();
        void AddPatient(Patient patient);
        void UpdatePatient(Patient patient);
        IEnumerable<Employee> GetAllDentists();
    }
}
