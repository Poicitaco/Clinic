using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string PatientName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public int DentistId { get; set; }
        public virtual Employee Dentist { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public AppointmentStatus Status { get; set; }

        public string Notes { get; set; }

        public int? CreatedById { get; set; }
        public virtual Employee CreatedBy { get; set; }
    }
}
