using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Interfaces
{
    public interface IInvoiceService
    {
        IEnumerable<Invoice> GetAllInvoices();
        Invoice GetInvoiceById(int id);
        void AddInvoice(Invoice invoice, List<InvoiceDetail> details);
        void PayInvoice(int id);
        void CancelInvoice(int id, string reason);
        void AddPayment(int invoiceId, decimal amount, string note, int employeeId);
        IEnumerable<PatientExamination> GetCompletedExaminationsWithoutInvoice();
        IEnumerable<Service> GetAllServices();
    }
}
