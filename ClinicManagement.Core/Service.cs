using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Core
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public int CategoryId { get; set; }
        public virtual ServiceCategory Category { get; set; }

        public bool IsMultiStage { get; set; }
        
        public virtual ICollection<ServiceStage> Stages { get; set; }

        public Service()
        {
            Stages = new HashSet<ServiceStage>();
        }

        [Required]
        public decimal Price { get; set; }

        public bool IsActive { get; set; }
    }
}
