using System.ComponentModel;

namespace ClinicManagement.Core
{
    public enum EmployeeRole
    {
        [Description("Quản lý")] Manager = 0,
        [Description("Nha sĩ")] Dentist = 1,
        [Description("Lễ tân")] Receptionist = 2
    }

    public enum Gender
    {
        [Description("Nam")] Male = 1,
        [Description("Nữ")] Female = 2,
        [Description("Khác")] Other = 3
    }

    public enum ContractStatus
    {
        [Description("Đang làm việc")] Working = 1,
        [Description("Đã nghỉ việc")] Resigned = 2
    }

    public enum AcademicDegree
    {
        [Description("Không có")] None = 0,
        [Description("Bác sĩ")] Doctor = 1,
        [Description("Thạc sĩ")] Master = 2,
        [Description("Tiến sĩ")] PhD = 3,
        [Description("Phó Giáo sư")] AssociateProfessor = 4,
        [Description("Giáo sư")] Professor = 5
    }

    public enum AppointmentStatus
    {
        [Description("Chờ khám")] Pending = 1,
        [Description("Đã hoàn thành")] Completed = 2,
        [Description("Đã hủy")] Cancelled = 3
    }

    public enum ExaminationStatus
    {
        [Description("Đang khám")] Draft = 1,
        [Description("Hoàn tất")] Finalized = 2
    }

    public enum InvoiceStatus
    {
        [Description("Chưa thanh toán")] Pending = 1,
        [Description("Đã thanh toán")] Paid = 2,
        [Description("Đã hủy")] Cancelled = 3
    }
}