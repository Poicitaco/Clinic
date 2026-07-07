using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ClinicManagement.Core;
using ClinicManagement.Business.Services;

namespace ClinicManagement.Tests
{
    [TestClass]
    public class ServiceServiceTests
    {
        private ServiceService _serviceService;
        private FakeUnitOfWork _fakeUow;

        [TestInitialize]
        public void Setup()
        {
            UserContext.CurrentUser = new Employee { Id = 1, Role = EmployeeRole.Manager };
            _fakeUow = new FakeUnitOfWork();
            _serviceService = new ServiceService(_fakeUow);
        }

        [TestMethod]
        public void AddService_MultiStageSumNot100_ThrowsException()
        {
            // Input: Service MultiStage, Stages tổng phần trăm = 90
            // Expected CHÍNH XÁC: Exception "Tổng phần trăm của các giai đoạn phải bằng 100%."
            var service = new Service
            {
                Name = "Trồng răng",
                Price = 5000,
                IsMultiStage = true,
                Stages = new List<ServiceStage>
                {
                    new ServiceStage { Name = "Giai đoạn 1", Percentage = 50 },
                    new ServiceStage { Name = "Giai đoạn 2", Percentage = 40 }
                }
            };

            var ex = Assert.ThrowsException<Exception>(() => _serviceService.AddService(service));
            Assert.AreEqual("Tổng phần trăm của các giai đoạn phải bằng 100%.", ex.Message);
        }

        [TestMethod]
        public void GetAllCategories_DoesNotCreateDefaultCategory()
        {
            var categories = _serviceService.GetAllCategories().ToList();

            Assert.AreEqual(0, categories.Count);
            Assert.AreEqual(0, _fakeUow.ServiceCategories.GetAll().Count());
        }

        [TestMethod]
        public void AddCategory_ValidCategory_ShouldCreateSuccessfully()
        {
            _serviceService.AddCategory(new ServiceCategory { Name = " Điều trị ", Description = "Nhóm điều trị" });

            var category = _fakeUow.ServiceCategories.GetAll().Single();
            Assert.AreEqual("Điều trị", category.Name);
            Assert.AreEqual("Nhóm điều trị", category.Description);
        }

        [TestMethod]
        public void AddCategory_DuplicateName_ThrowsException()
        {
            _fakeUow.ServiceCategories.Add(new ServiceCategory { Id = 1, Name = "Điều trị" });

            var ex = Assert.ThrowsException<Exception>(() =>
                _serviceService.AddCategory(new ServiceCategory { Name = "điều trị" }));

            Assert.IsTrue(ex.Message.Contains("không được trùng"));
        }

        [TestMethod]
        public void UpdateCategory_ValidCategory_ShouldUpdateNameAndDescription()
        {
            _fakeUow.ServiceCategories.Add(new ServiceCategory { Id = 1, Name = "Cũ", Description = "Mô tả cũ" });

            _serviceService.UpdateCategory(new ServiceCategory { Id = 1, Name = "Mới", Description = "Mô tả mới" });

            var category = _fakeUow.ServiceCategories.GetById(1);
            Assert.AreEqual("Mới", category.Name);
            Assert.AreEqual("Mô tả mới", category.Description);
        }

        [TestMethod]
        public void AddCategory_RoleReceptionist_ThrowsUnauthorized()
        {
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
                _serviceService.AddCategory(new ServiceCategory { Name = "Điều trị" }));

            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }

        [TestMethod]
        public void AddService_DoesNotRequirePrice()
        {
            var service = new Service
            {
                Name = "Khám răng",
                CategoryId = 1,
                IsActive = true
            };

            _serviceService.AddService(service);

            Assert.AreEqual(1, _fakeUow.Services.GetAll().Count());
            Assert.AreEqual(0, service.Price);
        }

        [TestMethod]
        public void UpdateService_DoesNotChangePrice()
        {
            _fakeUow.Services.Add(new Service
            {
                Id = 1,
                Name = "Khám răng",
                CategoryId = 1,
                Price = 100000,
                IsActive = true
            });

            _serviceService.UpdateService(new Service
            {
                Id = 1,
                Name = "Khám tổng quát",
                CategoryId = 1,
                Price = 999999,
                IsActive = true
            });

            var service = _fakeUow.Services.GetById(1);
            Assert.AreEqual("Khám tổng quát", service.Name);
            Assert.AreEqual(100000, service.Price);
        }

        [TestMethod]
        public void UpdateService_UpdatesDescription()
        {
            _fakeUow.Services.Add(new Service
            {
                Id = 1,
                Name = "Khám răng",
                Description = "Mô tả cũ",
                CategoryId = 1,
                IsActive = true
            });

            _serviceService.UpdateService(new Service
            {
                Id = 1,
                Name = "Khám răng",
                Description = "Mô tả mới",
                CategoryId = 1,
                IsActive = true
            });

            Assert.AreEqual("Mô tả mới", _fakeUow.Services.GetById(1).Description);
        }

        [TestMethod]
        public void SetServicePrice_MultipleCalls_KeepsAllHistoryRecords()
        {
            // Input: Thêm 3 mốc giá khác nhau
            // Expected CHÍNH XÁC: 3 bản ghi lịch sử được tạo, không bản ghi nào bị xóa hay ghi đè
            var service = new Service { Id = 1, Name = "Nhổ răng", Price = 100 };
            _fakeUow.Services.Add(service);

            _serviceService.SetServicePrice(1, 150, DateTime.Now.AddDays(1));
            _serviceService.SetServicePrice(1, 200, DateTime.Now.AddDays(2));
            _serviceService.SetServicePrice(1, 250, DateTime.Now.AddDays(3));

            var history = _serviceService.GetServicePriceHistory(1).ToList();
            Assert.AreEqual(3, history.Count);
        }

        [TestMethod]
        public void GetCurrentPrice_WithFutureEffectiveDate_ReturnsOldPrice()
        {
            // Input: Đặt giá mới với EffectiveDate ở tương lai (ngày mai)
            // Expected CHÍNH XÁC: Giá của dịch vụ vẫn giữ nguyên giá cũ (chưa áp dụng giá tương lai)
            var service = new Service { Id = 1, Name = "Nhổ răng", Price = 100 };
            _fakeUow.Services.Add(service);

            _serviceService.SetServicePrice(1, 200, DateTime.Now.AddDays(1)); // Giá áp dụng vào ngày mai

            // Giá hiện tại của Service vẫn phải là 100, không bị update thành 200
            Assert.AreEqual(100, _fakeUow.Services.GetById(1).Price);
            
            // Lịch sử vẫn lưu giá tương lai
            var history = _serviceService.GetServicePriceHistory(1).ToList();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(200, history[0].Price);
        }

        [TestMethod]
        public void SetServicePrice_Today_ThrowsException()
        {
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Nhổ răng", Price = 100 });

            var ex = Assert.ThrowsException<Exception>(() => _serviceService.SetServicePrice(1, 200, DateTime.Today));

            Assert.IsTrue(ex.Message.Contains("Ngày có hiệu lực"));
            Assert.AreEqual(0, _fakeUow.ServicePriceHistories.GetAll().Count());
        }

        [TestMethod]
        public void SetServicePrice_PastDate_ThrowsException()
        {
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Nhổ răng", Price = 100 });

            var ex = Assert.ThrowsException<Exception>(() => _serviceService.SetServicePrice(1, 200, DateTime.Today.AddDays(-1)));

            Assert.IsTrue(ex.Message.Contains("Ngày có hiệu lực"));
            Assert.AreEqual(0, _fakeUow.ServicePriceHistories.GetAll().Count());
        }

        [TestMethod]
        public void GetEffectivePrice_UsesLatestPriceNotAfterToday()
        {
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Nhổ răng", Price = 100 });
            _fakeUow.ServicePriceHistories.Add(new ServicePriceHistory
            {
                ServiceId = 1,
                Price = 150,
                EffectiveDate = DateTime.Today.AddDays(-2),
                CreatedDate = DateTime.Now.AddDays(-2)
            });
            _fakeUow.ServicePriceHistories.Add(new ServicePriceHistory
            {
                ServiceId = 1,
                Price = 200,
                EffectiveDate = DateTime.Today,
                CreatedDate = DateTime.Now
            });
            _fakeUow.ServicePriceHistories.Add(new ServicePriceHistory
            {
                ServiceId = 1,
                Price = 300,
                EffectiveDate = DateTime.Today.AddDays(1),
                CreatedDate = DateTime.Now
            });

            Assert.AreEqual(200, _serviceService.GetEffectivePrice(1));
            Assert.AreEqual(200, _serviceService.GetAllServices().Single().Price);
        }

        [TestMethod]
        public void PauseAndReopenService_TogglesIsActive()
        {
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Lấy cao răng", IsActive = true });

            _serviceService.PauseService(1);
            Assert.IsFalse(_fakeUow.Services.GetById(1).IsActive);

            _serviceService.ReopenService(1);
            Assert.IsTrue(_fakeUow.Services.GetById(1).IsActive);
        }

        [TestMethod]
        public void PauseService_RoleReceptionist_ThrowsUnauthorized()
        {
            _fakeUow.Services.Add(new Service { Id = 1, Name = "Lấy cao răng", IsActive = true });
            UserContext.CurrentUser = new Employee { Id = 2, Role = EmployeeRole.Receptionist };

            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => _serviceService.PauseService(1));

            Assert.IsTrue(ex.Message.Contains("không có quyền"));
        }
    }
}
