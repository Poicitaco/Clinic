using System;

namespace ClinicManagement.Core
{
    public class PatientExamination
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        public int? AppointmentId { get; set; }
        public virtual Appointment Appointment { get; set; }

        public int DentistId { get; set; }
        public virtual Employee Dentist { get; set; }

        public DateTime ExaminationDate { get; set; }

        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string TreatmentPlan { get; set; }
        public string Prescription { get; set; }
        public string ProposedServices { get; set; }

        public string DentalChartDetails { get; set; }

        public string Notes { get; set; }
        public DateTime? ReExamDate { get; set; }
        public string ManagerInterventionReason { get; set; }

        public ExaminationStatus Status { get; set; }
    }
}
