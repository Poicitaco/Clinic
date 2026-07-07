using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ClinicManagement.Core;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class StatisticServiceTests
    {
        private StatisticService _statisticService;
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            _fakeUow = new FakeUnitOfWork();
            _statisticService = new StatisticService(_fakeUow);
            UserContext.CurrentUser = new Employee { Role = EmployeeRole.Manager };

            // Add Invoices
            _fakeUow.Invoices.Add(new Invoice { Id = 1, TotalAmount = 1000, Status = InvoiceStatus.Paid, CreatedDate = DateTime.Today });
            _fakeUow.Invoices.Add(new Invoice { Id = 2, TotalAmount = 2000, Status = InvoiceStatus.Paid, CreatedDate = DateTime.Today });
            _fakeUow.Invoices.Add(new Invoice { Id = 3, TotalAmount = 5000, Status = InvoiceStatus.Cancelled, CreatedDate = DateTime.Today });
            _fakeUow.Invoices.Add(new Invoice { Id = 4, TotalAmount = 3000, Status = InvoiceStatus.Pending, CreatedDate = DateTime.Today });
        }

        [TestMethod]
        public void GetRevenue_OnlyIncludesPaidInvoices()
        {
            // Input: Có 4 hóa đơn (2 Paid, 1 Canceled, 1 Pending)
            // Expected CHÍNH XÁC: Tổng doanh thu chỉ tính 2 hóa đơn Paid (1000 + 2000 = 3000)
            var total = _statisticService.GetTotalRevenue(DateTime.Today, DateTime.Today);
            Assert.AreEqual(3000, total);
        }

        [TestMethod]
        public void GetRevenue_ExcludesCanceledInvoices()
        {
            // Input: Có 4 hóa đơn (2 Paid, 1 Canceled, 1 Pending)
            // Expected CHÍNH XÁC: Số lượng hóa đơn Paid được đếm phải là 2, hóa đơn Hủy bị bỏ qua
            var count = _statisticService.GetPaidInvoiceCount(DateTime.Today, DateTime.Today);
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void GetRevenue_StartAfterEnd_ThrowsException()
        {
            var ex = Assert.ThrowsException<ArgumentException>(() =>
                _statisticService.GetTotalRevenue(DateTime.Today, DateTime.Today.AddDays(-1)));

            Assert.IsTrue(ex.Message.Contains("Ngày bắt đầu"));
        }

        [TestMethod]
        public void GetRevenue_RangeOver12Months_ThrowsException()
        {
            var ex = Assert.ThrowsException<ArgumentException>(() =>
                _statisticService.GetTotalRevenue(DateTime.Today, DateTime.Today.AddMonths(12).AddDays(1)));

            Assert.IsTrue(ex.Message.Contains("12 tháng"));
        }

        [TestMethod]
        public void GetAverageRevenuePerPaidInvoice_ReturnsAveragePaidOnly()
        {
            var average = _statisticService.GetAverageRevenuePerPaidInvoice(DateTime.Today, DateTime.Today);

            Assert.AreEqual(1500, average);
        }
    }
}
