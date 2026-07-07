using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class SalaryConfiguration
    {
        public int Id { get; set; }

        [Range(1000, double.MaxValue)]
        public decimal HourlyRate { get; set; }

        [Range(1.0, 1.5)]
        public decimal DefaultShiftCoefficient { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal ReceptionistCoefficient { get; set; }

        public virtual ICollection<DentistDegreeSalaryCoefficient> DentistDegreeCoefficients { get; set; }

        public SalaryConfiguration()
        {
            DentistDegreeCoefficients = new HashSet<DentistDegreeSalaryCoefficient>();
            DefaultShiftCoefficient = 1.0m;
            ReceptionistCoefficient = 1.0m;
        }
    }
}
