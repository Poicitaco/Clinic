using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Core
{
    public class DentistDegreeSalaryCoefficient
    {
        public int Id { get; set; }

        [Required]
        public int SalaryConfigurationId { get; set; }

        [ForeignKey("SalaryConfigurationId")]
        public virtual SalaryConfiguration SalaryConfiguration { get; set; }

        [Required]
        public AcademicDegree Degree { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal Coefficient { get; set; }
    }
}
