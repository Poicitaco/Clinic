using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class Patient
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

        [EmailAddress]
        public string Email { get; set; }

        [MaxLength(255)]
        public string Address { get; set; }
    }
}
