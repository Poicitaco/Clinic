using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Core
{
    public class WorkShift
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [NotMapped]
        public decimal TotalHours => (decimal)(EndTime - StartTime).TotalHours;

        public virtual ICollection<EmployeeSchedule> Schedules { get; set; }

        public WorkShift()
        {
            Schedules = new HashSet<EmployeeSchedule>();
        }
    }
}
