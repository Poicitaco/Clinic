using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class ServicePriceHistory
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public decimal Price { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual Service Service { get; set; }
    }
}
