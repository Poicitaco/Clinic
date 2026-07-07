using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MinLength(2)]
        public string FullName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [MaxLength(255)]
        public string Address { get; set; }

        public string AvatarUrl { get; set; }

        public EmployeeRole Role { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public ContractStatus ContractStatus { get; set; }

        public AcademicDegree? Degree { get; set; }

        public DateTime? ResignationDate { get; set; }

        public virtual Account Account { get; set; }
    }
}