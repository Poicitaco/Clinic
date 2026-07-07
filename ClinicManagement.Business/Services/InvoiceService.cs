using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Invoice> GetAllInvoices()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            return _unitOfWork.Invoices.AsQueryable()
                .Include(i => i.Patient)
                .Include(i => i.Examination)
                .Include(i => i.CreatedBy)
                .OrderByDescending(i => i.CreatedDate)
                .ToList();
        }

        public Invoice GetInvoiceById(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            return _unitOfWork.Invoices.GetById(id);
        }

        public void AddInvoice(Invoice invoice, List<InvoiceDetail> details)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            if (details == null || !details.Any())
                throw new Exception("Hóa đơn phải có ít nhất 1 dịch vụ.");

            if (invoice.ExaminationId.HasValue && _unitOfWork.Invoices.Find(i => i.ExaminationId == invoice.ExaminationId.Value).Any())
                throw new Exception("Đợt khám này đã có hóa đơn.");

            if (invoice.ExaminationId.HasValue)
            {
                var examination = _unitOfWork.PatientExaminations.GetById(invoice.ExaminationId.Value);
                if (examination == null)
                    throw new Exception("Không tìm thấy bệnh án.");

                invoice.PatientId = examination.PatientId;
            }

            if (invoice.PatientId <= 0)
                throw new Exception("Hóa đơn phải gắn với bệnh nhân.");

            foreach (var detail in details)
            {
                var service = _unitOfWork.Services.GetById(detail.ServiceId);
                if (service == null || !service.IsActive)
                    throw new Exception("Dịch vụ không tồn tại hoặc đã tạm dừng.");
                if (detail.Quantity <= 0)
                    throw new Exception("Số lượng dịch vụ phải lớn hơn 0.");
                if (detail.UnitPrice <= 0)
                    throw new Exception("Đơn giá dịch vụ phải lớn hơn 0.");

                var standardPrice = GetEffectiveServicePrice(detail.ServiceId, service);
                if (UserContext.CurrentUser.Role != EmployeeRole.Manager && detail.UnitPrice != standardPrice)
                    throw new UnauthorizedAccessException("Chỉ Quản lý mới được áp dụng giá ưu đãi cho hóa đơn.");
            }

            invoice.CreatedDate = DateTime.Now;
            invoice.CreatedById = UserContext.CurrentUser.Id;
            invoice.Status = InvoiceStatus.Pending;
            invoice.TotalAmount = details.Sum(d => d.UnitPrice * d.Quantity);
            
            _unitOfWork.Invoices.Add(invoice);
            _unitOfWork.Complete();

            foreach (var detail in details)
            {
                detail.InvoiceId = invoice.Id;
                detail.Amount = detail.UnitPrice * detail.Quantity;
                _unitOfWork.InvoiceDetails.Add(detail);
            }
            
            _unitOfWork.Complete();
        }

        public void PayInvoice(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var invoice = _unitOfWork.Invoices.GetById(id);
            if (invoice == null)
                throw new Exception("Không tìm thấy hóa đơn.");

            if (invoice.Status != InvoiceStatus.Pending)
                throw new Exception("Chỉ hóa đơn chờ thanh toán mới được thanh toán.");

            invoice.Status = InvoiceStatus.Paid;
            _unitOfWork.Complete();
        }

        public void CancelInvoice(int id, string reason)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var invoice = _unitOfWork.Invoices.GetById(id);
            if (invoice == null)
                throw new Exception("Không tìm thấy hóa đơn.");

            if (invoice.Status != InvoiceStatus.Pending)
                throw new Exception("Chỉ hóa đơn chờ thanh toán mới được hủy.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Lý do hủy hóa đơn là bắt buộc.");

            invoice.Status = InvoiceStatus.Cancelled;
            var cancelLog = $"Hủy bởi nhân viên #{UserContext.CurrentUser.Id} lúc {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {reason.Trim()}";
            invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? cancelLog : invoice.Notes + Environment.NewLine + cancelLog;
            _unitOfWork.Complete();
        }

        public void AddPayment(int invoiceId, decimal amount, string note, int employeeId)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var invoice = _unitOfWork.Invoices.GetById(invoiceId);
            if (invoice == null) throw new Exception("Không tìm thấy hóa đơn.");
            if (invoice.Status != InvoiceStatus.Pending) throw new Exception("Chỉ hóa đơn chờ thanh toán mới được thu tiền.");
            
            if (amount <= 0) throw new Exception("Số tiền thanh toán phải lớn hơn 0.");

            var payment = new InvoicePayment
            {
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                CreatedById = employeeId,
                Note = note
            };

            _unitOfWork.InvoicePayments.Add(payment);

            invoice.PaidAmount += amount;

            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else
            {
                invoice.Status = InvoiceStatus.Pending; // Or partial
            }

            _unitOfWork.Complete();
        }

        public IEnumerable<PatientExamination> GetCompletedExaminationsWithoutInvoice()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            // Lấy các bệnh án đã hoàn thành nhưng chưa có hóa đơn
            var invoicedExamIds = _unitOfWork.Invoices.GetAll()
                                    .Where(i => i.ExaminationId.HasValue)
                                    .Select(i => i.ExaminationId.Value)
                                    .ToList();

            return _unitOfWork.PatientExaminations
                .AsQueryable()
                .Include(e => e.Patient)
                .Where(e => e.Status == ExaminationStatus.Finalized && !invoicedExamIds.Contains(e.Id))
                .ToList();
        }

        public IEnumerable<Service> GetAllServices()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            return _unitOfWork.Services.Find(s => s.IsActive).ToList();
        }

        private decimal GetEffectiveServicePrice(int serviceId, Service service)
        {
            var today = DateTime.Today;
            var currentPrice = _unitOfWork.ServicePriceHistories
                .Find(h => h.ServiceId == serviceId && h.EffectiveDate <= today)
                .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.CreatedDate)
                .Select(h => (decimal?)h.Price)
                .FirstOrDefault();

            return currentPrice ?? service.Price;
        }
    }
}
