using System;
using System.Collections.Generic;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private IQueryable<Invoice> GetPaidInvoices(DateTime startDate, DateTime endDate)
        {
            ValidateDateRange(startDate, endDate);
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
            return _unitOfWork.Invoices.Find(i => i.Status == InvoiceStatus.Paid && i.CreatedDate >= startDate.Date && i.CreatedDate <= endOfDay).AsQueryable();
        }

        private void ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
                throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");

            if (endDate.Date > startDate.Date.AddMonths(12))
                throw new ArgumentException("Khoảng thời gian thống kê không được vượt quá 12 tháng.");
        }

        public decimal GetTotalRevenue(DateTime startDate, DateTime endDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var invoices = GetPaidInvoices(startDate, endDate);
            return invoices.Any() ? invoices.Sum(i => i.TotalAmount) : 0;
        }

        public int GetPaidInvoiceCount(DateTime startDate, DateTime endDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            return GetPaidInvoices(startDate, endDate).Count();
        }

        public decimal GetAverageRevenuePerPaidInvoice(DateTime startDate, DateTime endDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var invoices = GetPaidInvoices(startDate, endDate).ToList();
            return invoices.Any() ? invoices.Average(i => i.TotalAmount) : 0;
        }

        public List<RevenueData> GetRevenueByTime(DateTime startDate, DateTime endDate, string groupBy)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var invoices = GetPaidInvoices(startDate, endDate).ToList();
            if (groupBy == "Month")
            {
                return invoices.GroupBy(i => new { i.CreatedDate.Year, i.CreatedDate.Month })
                    .Select(g => new RevenueData
                    {
                        TimeLabel = $"{g.Key.Month}/{g.Key.Year}",
                        TotalRevenue = g.Sum(i => i.TotalAmount)
                    })
                    .OrderBy(x => x.TimeLabel)
                    .ToList();
            }
            else // Day
            {
                return invoices.GroupBy(i => i.CreatedDate.Date)
                    .Select(g => new RevenueData
                    {
                        TimeLabel = g.Key.ToString("dd/MM/yyyy"),
                        TotalRevenue = g.Sum(i => i.TotalAmount)
                    })
                    .OrderBy(x => DateTime.ParseExact(x.TimeLabel, "dd/MM/yyyy", null))
                    .ToList();
            }
        }

        public List<CategoryRevenueData> GetRevenueByCategory(DateTime startDate, DateTime endDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var invoiceDetails = _unitOfWork.InvoiceDetails.GetAll().ToList();
            var invoices = GetPaidInvoices(startDate, endDate).Select(i => i.Id).ToList();

            var relevantDetails = invoiceDetails.Where(d => invoices.Contains(d.InvoiceId)).ToList();
            var totalRevenue = relevantDetails.Sum(d => d.Amount);

            if (totalRevenue == 0) return new List<CategoryRevenueData>();

            return relevantDetails.GroupBy(d => d.Service.Category.Name)
                .Select(g => new CategoryRevenueData
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(d => d.Amount),
                    Percentage = (double)(g.Sum(d => d.Amount) / totalRevenue * 100)
                })
                .ToList();
        }

        public List<InvoiceReportData> GetInvoiceReport(DateTime startDate, DateTime endDate, int? categoryId, int? serviceId, int? dentistId)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            ValidateDateRange(startDate, endDate);
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
            var invoices = _unitOfWork.Invoices.Find(i => i.Status == InvoiceStatus.Paid && i.CreatedDate >= startDate.Date && i.CreatedDate <= endOfDay).ToList();
            var details = _unitOfWork.InvoiceDetails.GetAll().ToList();

            var query = details.Where(d => invoices.Select(i => i.Id).Contains(d.InvoiceId));

            if (categoryId.HasValue)
                query = query.Where(d => d.Service.CategoryId == categoryId.Value);
            
            if (serviceId.HasValue)
                query = query.Where(d => d.ServiceId == serviceId.Value);

            if (dentistId.HasValue)
                query = query.Where(d => d.Invoice.Examination != null && d.Invoice.Examination.DentistId == dentistId.Value);

            return query.GroupBy(d => new { 
                d.Invoice.CreatedDate.Date, 
                CatName = d.Service.Category.Name, 
                SrvName = d.Service.Name, 
                DenName = d.Invoice.Examination?.Dentist?.FullName ?? "N/A"
            }).Select(g => new InvoiceReportData
            {
                TimeLabel = g.Key.Date.ToString("dd/MM/yyyy"),
                CategoryName = g.Key.CatName,
                ServiceName = g.Key.SrvName,
                DentistName = g.Key.DenName,
                InvoiceCount = g.Select(d => d.InvoiceId).Distinct().Count(),
                Revenue = g.Sum(d => d.Amount)
            }).ToList();
        }
    }
}
