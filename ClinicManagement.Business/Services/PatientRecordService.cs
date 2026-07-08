using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class PatientRecordService : IPatientRecordService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PatientRecordService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<PatientExamination> GetAllRecords()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Dentist);

            var query = _unitOfWork.PatientExaminations.AsQueryable()
                .Include(r => r.Patient)
                .Include(r => r.Dentist)
                .Include(r => r.Appointment);

            if (UserContext.CurrentUser.Role == EmployeeRole.Dentist)
            {
                query = query.Where(r => r.DentistId == UserContext.CurrentUser.Id);
            }

            return query.OrderByDescending(r => r.ExaminationDate).ToList();
        }

        public PatientExamination GetRecordById(int id)
        {
            return _unitOfWork.PatientExaminations.GetById(id);
        }

        public void AddRecord(PatientExamination record)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Dentist);

            if (UserContext.CurrentUser.Role == EmployeeRole.Dentist)
            {
                record.DentistId = UserContext.CurrentUser.Id;
            }

            if (string.IsNullOrWhiteSpace(record.Diagnosis))
                throw new Exception("Chẩn đoán không được để trống.");

            record.ExaminationDate = DateTime.Today;

            _unitOfWork.PatientExaminations.Add(record);
            _unitOfWork.Complete();
        }

        public void UpdateRecord(PatientExamination record)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Dentist);

            if (string.IsNullOrWhiteSpace(record.Diagnosis))
                throw new Exception("Chẩn đoán không được để trống.");

            var existingRecord = _unitOfWork.PatientExaminations.GetById(record.Id);
            if (existingRecord != null)
            {
                if (UserContext.CurrentUser.Role == EmployeeRole.Dentist && existingRecord.DentistId != UserContext.CurrentUser.Id)
                {
                    throw new UnauthorizedAccessException("Nha sĩ chỉ được sửa bệnh án của chính mình.");
                }

                var isManager = UserContext.CurrentUser.Role == EmployeeRole.Manager;
                if (existingRecord.Status == ExaminationStatus.Finalized && !isManager)
                {
                    throw new Exception("Không thể sửa bệnh án đã hoàn thành (chốt).");
                }

                if (existingRecord.Status == ExaminationStatus.Finalized && string.IsNullOrWhiteSpace(record.ManagerInterventionReason))
                {
                    throw new Exception("Quản lý phải nhập lý do can thiệp khi sửa bệnh án đã chốt.");
                }

                existingRecord.Symptoms = record.Symptoms;
                existingRecord.Diagnosis = record.Diagnosis;
                existingRecord.TreatmentPlan = record.TreatmentPlan;
                existingRecord.Prescription = record.Prescription;
                existingRecord.ProposedServices = record.ProposedServices;
                existingRecord.DentalChartDetails = record.DentalChartDetails;
                existingRecord.Notes = record.Notes;
                existingRecord.ReExamDate = record.ReExamDate;
                if (existingRecord.Status == ExaminationStatus.Finalized)
                {
                    existingRecord.ManagerInterventionReason = record.ManagerInterventionReason;
                }
                else
                {
                    existingRecord.Status = record.Status;
                }

                _unitOfWork.Complete();
            }
        }

        public IEnumerable<Patient> GetAllPatients()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Dentist, EmployeeRole.Receptionist);

            var patients = _unitOfWork.Patients.GetAll().ToList();
            if (!patients.Any())
            {
                // Sinh mặc định nếu trống
                var defaultPatient = new Patient { FullName = "Bệnh nhân mẫu", PhoneNumber = "0987654321", DateOfBirth = new DateTime(1990, 1, 1), Gender = Gender.Male };
                _unitOfWork.Patients.Add(defaultPatient);
                _unitOfWork.Complete();
                patients.Add(defaultPatient);
            }
            return patients;
        }

        public void AddPatient(Patient patient)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);
            ValidatePatient(patient, null);

            _unitOfWork.Patients.Add(patient);
            _unitOfWork.Complete();
        }

        public void UpdatePatient(Patient patient)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);
            ValidatePatient(patient, patient.Id);

            var existing = _unitOfWork.Patients.GetById(patient.Id);
            if (existing == null)
                throw new Exception("Không tìm thấy bệnh nhân.");

            existing.FullName = patient.FullName.Trim();
            existing.DateOfBirth = patient.DateOfBirth;
            existing.Gender = patient.Gender;
            existing.PhoneNumber = patient.PhoneNumber;
            existing.Email = patient.Email;
            existing.Address = patient.Address;
            _unitOfWork.Complete();
        }

        private void ValidatePatient(Patient patient, int? currentId)
        {
            if (patient == null || string.IsNullOrWhiteSpace(patient.FullName))
                throw new Exception("Tên bệnh nhân không được để trống.");

            if (string.IsNullOrWhiteSpace(patient.PhoneNumber) || !System.Text.RegularExpressions.Regex.IsMatch(patient.PhoneNumber, @"^0\d{9}$"))
                throw new Exception("Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");

            if (_unitOfWork.Patients.GetAll().Any(p => (!currentId.HasValue || p.Id != currentId.Value) && p.PhoneNumber == patient.PhoneNumber))
                throw new Exception("Số điện thoại bệnh nhân không được trùng.");

            patient.FullName = patient.FullName.Trim();
        }

        public IEnumerable<Employee> GetAllDentists()
        {
            if (UserContext.CurrentUser != null && UserContext.CurrentUser.Role == EmployeeRole.Dentist)
            {
                return _unitOfWork.Employees.Find(e => e.Id == UserContext.CurrentUser.Id).ToList();
            }

            return _unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Dentist).ToList();
        }
    }
}
