using System;
using System.Collections.Generic;

namespace ClinicManagement.Core
{
    public class Invoice
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        public int? ExaminationId { get; set; }
        public virtual PatientExamination Examination { get; set; }

        public DateTime CreatedDate { get; set; }

        public int CreatedById { get; set; }
        public virtual Employee CreatedBy { get; set; }

        public string Notes { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public InvoiceStatus Status { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }

        public virtual ICollection<InvoicePayment> Payments { get; set; }

        public Invoice()
        {
            InvoiceDetails = new HashSet<InvoiceDetail>();
            Payments = new HashSet<InvoicePayment>();
        }
    }
}
