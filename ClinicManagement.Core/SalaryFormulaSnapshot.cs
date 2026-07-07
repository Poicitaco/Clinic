using System;

namespace ClinicManagement.Core
{
    public class SalaryFormulaSnapshot
    {
        public int Id { get; set; }
        public int SalaryRecordId { get; set; }
        public EmployeeRole Role { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Allowance { get; set; }
        public DateTime SnapshotDate { get; set; }
        
        public SalaryRecord SalaryRecord { get; set; }
    }
}
