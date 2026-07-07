using System;

namespace ClinicManagement.Core
{
    public class SalaryRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsFinalized { get; set; }
        public DateTime? FinalizedDate { get; set; }
        
        public Employee Employee { get; set; }
        public SalaryFormulaSnapshot FormulaSnapshot { get; set; }
    }
}
