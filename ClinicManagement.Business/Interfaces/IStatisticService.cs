using System;
using System.Collections.Generic;

namespace ClinicManagement.Business.Interfaces
{
    public class RevenueData
    {
        public string TimeLabel { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CategoryRevenueData
    {
        public string CategoryName { get; set; }
        public decimal TotalRevenue { get; set; }
        public double Percentage { get; set; }
    }

    public class InvoiceReportData
    {
        public string TimeLabel { get; set; }
        public string CategoryName { get; set; }
        public string ServiceName { get; set; }
        public string DentistName { get; set; }
        public int InvoiceCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public interface IStatisticService
    {
        decimal GetTotalRevenue(DateTime startDate, DateTime endDate);
        decimal GetAverageRevenuePerPaidInvoice(DateTime startDate, DateTime endDate);
        int GetPaidInvoiceCount(DateTime startDate, DateTime endDate);
        List<RevenueData> GetRevenueByTime(DateTime startDate, DateTime endDate, string groupBy);
        List<CategoryRevenueData> GetRevenueByCategory(DateTime startDate, DateTime endDate);
        List<InvoiceReportData> GetInvoiceReport(DateTime startDate, DateTime endDate, int? categoryId, int? serviceId, int? dentistId);
    }
}
