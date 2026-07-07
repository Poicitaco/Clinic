using System;
using System.Collections.Generic;
using ClinicManagement.Core;
namespace ClinicManagement.Business.Interfaces
{
    public interface IServiceService
    {
        IEnumerable<Service> GetAllServices();
        IEnumerable<ServiceCategory> GetAllCategories();
        void AddCategory(ServiceCategory category);
        void UpdateCategory(ServiceCategory category);
        Service GetServiceById(int id);
        void AddService(Service service);
        void UpdateService(Service service);
        void PauseService(int id);
        void ReopenService(int id);
        void DeleteService(int id);
        
        void SetServicePrice(int serviceId, decimal price, DateTime effectiveDate);
        decimal GetEffectivePrice(int serviceId);
        IEnumerable<ServicePriceHistory> GetServicePriceHistory(int serviceId);
    }
}
