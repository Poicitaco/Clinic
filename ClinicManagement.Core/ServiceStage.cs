namespace ClinicManagement.Core
{
    public class ServiceStage
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } // VD: Giai đoạn 1, Lắp mắc cài...
        public int Order { get; set; }
        public decimal Percentage { get; set; } // % tổng chi phí dịch vụ, tổng các stage = 100%

        public virtual Service Service { get; set; }
    }
}
