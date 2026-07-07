using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Core
{
    public class EmployeeSchedule
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [Required]
        public int WorkShiftId { get; set; }

        [ForeignKey("WorkShiftId")]
        public virtual WorkShift WorkShift { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ScheduleDate { get; set; }

        // Mặc định null để phân biệt giữa "đã nhập" và "sử dụng cấu hình mặc định"
        public float? ShiftCoefficient { get; set; }

        public float PatientCoefficient { get; set; }

        public EmployeeSchedule()
        {
            PatientCoefficient = 0f;
        }
    }
}
