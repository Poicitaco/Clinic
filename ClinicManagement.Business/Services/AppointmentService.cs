using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Appointment> GetAllAppointments()
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var query = _unitOfWork.Appointments.AsQueryable()
                .Include(a => a.Dentist)
                .Include(a => a.CreatedBy);

            return query.OrderBy(a => a.StartTime).ToList();
        }

        public IEnumerable<Appointment> GetAppointmentsByDate(DateTime date)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var query = _unitOfWork.Appointments.AsQueryable()
                .Include(a => a.Dentist)
                .Where(a => a.StartTime.Year == date.Year && a.StartTime.Month == date.Month && a.StartTime.Day == date.Day);

            return query
                .OrderBy(a => a.StartTime)
                .ToList();
        }

        public void AddAppointment(Appointment appointment)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            ValidateAppointment(appointment, allowPastStartTime: false);
            EnsureDentistIsAvailable(appointment);
            appointment.CreatedById = UserContext.CurrentUser.Id;

            _unitOfWork.Appointments.Add(appointment);
            _unitOfWork.Complete();
        }

        public void UpdateAppointment(Appointment appointment)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var existing = _unitOfWork.Appointments.GetById(appointment.Id);
            if (existing != null)
            {
                var isPastAppointment = existing.StartTime < DateTime.Now;
                ValidateAppointment(appointment, allowPastStartTime: isPastAppointment);

                if (!isPastAppointment)
                    EnsureDentistIsAvailable(appointment);

                existing.PatientName = appointment.PatientName;
                existing.PhoneNumber = appointment.PhoneNumber;
                existing.DentistId = appointment.DentistId;
                existing.StartTime = appointment.StartTime;
                existing.EndTime = appointment.EndTime;
                existing.Status = appointment.Status;
                existing.Notes = appointment.Notes;
                
                _unitOfWork.Complete();
            }
        }

        private void ValidateAppointment(Appointment appointment, bool allowPastStartTime)
        {
            if (string.IsNullOrWhiteSpace(appointment.PatientName))
                throw new Exception("Tên bệnh nhân không được để trống.");

            var nameParts = appointment.PatientName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2 || !Regex.IsMatch(appointment.PatientName, @"^[\p{L}\s]+$"))
                throw new Exception("Tên bệnh nhân phải có tối thiểu 2 từ và không chứa số hoặc ký tự đặc biệt.");

            if (string.IsNullOrWhiteSpace(appointment.PhoneNumber) || !Regex.IsMatch(appointment.PhoneNumber, @"^0\d{9}$"))
                throw new Exception("Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");

            if (appointment.DentistId <= 0)
                throw new Exception("Vui lòng chọn Nha sĩ phụ trách.");

            if (!allowPastStartTime && appointment.StartTime < DateTime.Now)
                throw new Exception("Thời gian hẹn không thể ở quá khứ.");

            if (appointment.EndTime <= appointment.StartTime)
                throw new Exception("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

            if (!allowPastStartTime)
                EnsureDentistWorksDuring(appointment);
        }

        private void EnsureDentistWorksDuring(Appointment appointment)
        {
            var dentist = _unitOfWork.Employees.GetById(appointment.DentistId);
            if (dentist == null || dentist.Role != EmployeeRole.Dentist || dentist.ContractStatus != ContractStatus.Working)
                throw new Exception("Nha sĩ không hợp lệ hoặc không còn làm việc.");

            var scheduleDate = appointment.StartTime.Date;
            var start = appointment.StartTime.TimeOfDay;
            var end = appointment.EndTime.TimeOfDay;

            var worksDuring = _unitOfWork.EmployeeSchedules
                .Find(s => s.EmployeeId == appointment.DentistId && s.ScheduleDate == scheduleDate)
                .Any(s =>
                {
                    var shift = _unitOfWork.WorkShifts.GetById(s.WorkShiftId);
                    return shift != null && start >= shift.StartTime && end <= shift.EndTime;
                });

            if (!worksDuring)
                throw new Exception("Nha sĩ không có ca làm việc trong khung giờ này.");
        }

        private void EnsureDentistIsAvailable(Appointment appointment)
        {
            var hasOverlap = _unitOfWork.Appointments.Find(a =>
                a.Id != appointment.Id &&
                a.DentistId == appointment.DentistId &&
                a.Status != AppointmentStatus.Cancelled &&
                appointment.StartTime < a.EndTime &&
                a.StartTime < appointment.EndTime).Any();

            if (hasOverlap)
                throw new Exception("Nha sĩ đã có lịch hẹn trong khung giờ này.");
        }

        public void CancelAppointment(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);

            var existing = _unitOfWork.Appointments.GetById(id);
            if (existing != null)
            {
                if (existing.Status != AppointmentStatus.Pending)
                    throw new Exception("Chỉ lịch hẹn chưa diễn ra mới được hủy.");

                if (existing.StartTime < DateTime.Now.AddHours(1))
                    throw new Exception("Chỉ được hủy lịch hẹn trước giờ bắt đầu ít nhất 1 tiếng.");

                existing.Status = AppointmentStatus.Cancelled;
                _unitOfWork.Complete();
            }
        }
    }
}
