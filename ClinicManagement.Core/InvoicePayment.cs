using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class InvoicePayment
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public int CreatedById { get; set; }
        public virtual Employee CreatedBy { get; set; }

        [MaxLength(255)]
        public string Note { get; set; }
    }
}
