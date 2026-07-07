using System;
using System.Collections.Generic;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ServiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Service> GetAllServices()
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var services = _unitOfWork.Services.GetAll().ToList();
            foreach (var service in services)
            {
                service.Price = GetEffectivePrice(service.Id);
            }
            return services;
        }

        public IEnumerable<ServiceCategory> GetAllCategories()
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            return _unitOfWork.ServiceCategories.GetAll().ToList();
        }

        public void AddCategory(ServiceCategory category)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            ValidateCategory(category, null);

            _unitOfWork.ServiceCategories.Add(category);
            _unitOfWork.Complete();
        }

        public void UpdateCategory(ServiceCategory category)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            ValidateCategory(category, category.Id);

            var existing = _unitOfWork.ServiceCategories.GetById(category.Id);
            if (existing == null)
                throw new Exception("Không tìm thấy nhóm dịch vụ.");

            existing.Name = category.Name.Trim();
            existing.Description = category.Description;
            _unitOfWork.Complete();
        }

        private void ValidateCategory(ServiceCategory category, int? currentId)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
                throw new Exception("Tên nhóm dịch vụ không được để trống.");

            var name = category.Name.Trim();
            var duplicate = _unitOfWork.ServiceCategories.GetAll()
                .Any(c => (!currentId.HasValue || c.Id != currentId.Value)
                    && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new Exception("Tên nhóm dịch vụ không được trùng.");

            category.Name = name;
        }

        public Service GetServiceById(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var service = _unitOfWork.Services.GetById(id);
            if (service != null)
                service.Price = GetEffectivePrice(id);
            return service;
        }

        public void AddService(Service service)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            if (string.IsNullOrWhiteSpace(service.Name))
                throw new Exception("Tên dịch vụ không được để trống.");

            if (service.IsMultiStage)
            {
                if (service.Stages == null || !service.Stages.Any())
                    throw new Exception("Dịch vụ nhiều giai đoạn phải có ít nhất 1 giai đoạn.");
                
                decimal totalPercentage = service.Stages.Sum(s => s.Percentage);
                if (totalPercentage != 100m)
                    throw new Exception("Tổng phần trăm của các giai đoạn phải bằng 100%.");
            }

            _unitOfWork.Services.Add(service);
            _unitOfWork.Complete();
        }

        public void UpdateService(Service service)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            if (string.IsNullOrWhiteSpace(service.Name))
                throw new Exception("Tên dịch vụ không được để trống.");

            if (service.IsMultiStage)
            {
                if (service.Stages == null || !service.Stages.Any())
                    throw new Exception("Dịch vụ nhiều giai đoạn phải có ít nhất 1 giai đoạn.");
                
                decimal totalPercentage = service.Stages.Sum(s => s.Percentage);
                if (totalPercentage != 100m)
                    throw new Exception("Tổng phần trăm của các giai đoạn phải bằng 100%.");
            }

            var existingService = _unitOfWork.Services.GetById(service.Id);
            if (existingService != null)
            {
                existingService.Name = service.Name;
                existingService.CategoryId = service.CategoryId;
                existingService.IsActive = service.IsActive;
                existingService.IsMultiStage = service.IsMultiStage;

                // Cập nhật stages: Để đơn giản, xóa hết stage cũ và thêm stage mới
                var oldStages = _unitOfWork.ServiceStages.Find(s => s.ServiceId == service.Id).ToList();
                foreach(var old in oldStages)
                {
                    _unitOfWork.ServiceStages.Remove(old);
                }

                if (service.IsMultiStage && service.Stages != null)
                {
                    foreach(var stage in service.Stages)
                    {
                        stage.ServiceId = service.Id;
                        _unitOfWork.ServiceStages.Add(stage);
                    }
                }

                _unitOfWork.Complete();
            }
        }

        public void DeleteService(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            // Kiểm tra xem dịch vụ đã được dùng trong InvoiceDetail nào chưa
            var isUsed = _unitOfWork.InvoiceDetails.Find(d => d.ServiceId == id).Any();
            if (isUsed)
            {
                throw new Exception("Không thể xóa dịch vụ này vì đã được sử dụng trong Hóa đơn.");
            }

            var srv = _unitOfWork.Services.GetById(id);
            if (srv != null)
            {
                _unitOfWork.Services.Remove(srv);
                _unitOfWork.Complete();
            }
        }

        public void PauseService(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var service = _unitOfWork.Services.GetById(id);
            if (service == null)
                throw new Exception("Không tìm thấy dịch vụ.");
            if (!service.IsActive)
                throw new Exception("Dịch vụ đã tạm dừng.");

            service.IsActive = false;
            _unitOfWork.Complete();
        }

        public void ReopenService(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);
            var service = _unitOfWork.Services.GetById(id);
            if (service == null)
                throw new Exception("Không tìm thấy dịch vụ.");
            if (service.IsActive)
                throw new Exception("Dịch vụ đang hoạt động.");

            service.IsActive = true;
            _unitOfWork.Complete();
        }

        public void SetServicePrice(int serviceId, decimal price, DateTime effectiveDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            if (price <= 0)
                throw new Exception("Giá tổng phải lớn hơn 0");
            if (effectiveDate.Date <= DateTime.Now.Date)
                throw new Exception("Ngày có hiệu lực phải sau ngày hiện tại ít nhất 1 ngày.");

            var history = new ServicePriceHistory
            {
                ServiceId = serviceId,
                Price = price,
                EffectiveDate = effectiveDate.Date,
                CreatedDate = DateTime.Now
            };

            _unitOfWork.ServicePriceHistories.Add(history);
            _unitOfWork.Complete();
        }

        public decimal GetEffectivePrice(int serviceId)
        {
            var service = _unitOfWork.Services.GetById(serviceId);
            if (service == null)
                throw new Exception("Không tìm thấy dịch vụ.");

            var today = DateTime.Now.Date;
            var currentPrice = _unitOfWork.ServicePriceHistories
                .Find(h => h.ServiceId == serviceId && h.EffectiveDate <= today)
                .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.CreatedDate)
                .Select(h => (decimal?)h.Price)
                .FirstOrDefault();

            return currentPrice ?? service.Price;
        }

        public IEnumerable<ServicePriceHistory> GetServicePriceHistory(int serviceId)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            return _unitOfWork.ServicePriceHistories
                .Find(h => h.ServiceId == serviceId)
                .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.CreatedDate)
                .ToList();
        }
    }
}
