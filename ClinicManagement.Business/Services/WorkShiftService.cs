using System;
using System.Collections.Generic;
using System.Linq;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Core;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Business.Services
{
    public class WorkShiftService : IWorkShiftService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkShiftService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public List<WorkShift> GetAllWorkShifts()
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            return _unitOfWork.WorkShifts.GetAll().OrderBy(s => s.StartTime).ToList();
        }

        public WorkShift GetWorkShiftById(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            return _unitOfWork.WorkShifts.GetById(id);
        }

        public void AddWorkShift(WorkShift shift)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            if (shift.EndTime <= shift.StartTime)
            {
                throw new Exception("Giờ kết thúc phải sau giờ bắt đầu.");
            }

            var existingShifts = GetAllWorkShifts();
            foreach (var existing in existingShifts)
            {
                // Kiểm tra chồng chéo
                if (shift.StartTime < existing.EndTime && existing.StartTime < shift.EndTime)
                {
                    throw new Exception($"Khung giờ trùng với ca {existing.Name}.");
                }
            }

            _unitOfWork.WorkShifts.Add(shift);
            _unitOfWork.Complete();
        }

        public void UpdateWorkShift(WorkShift shift)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            if (shift.EndTime <= shift.StartTime)
            {
                throw new Exception("Giờ kết thúc phải sau giờ bắt đầu.");
            }

            var existingShifts = GetAllWorkShifts().Where(s => s.Id != shift.Id);
            foreach (var existing in existingShifts)
            {
                if (shift.StartTime < existing.EndTime && existing.StartTime < shift.EndTime)
                {
                    throw new Exception($"Khung giờ trùng với ca {existing.Name}.");
                }
            }

            var entity = _unitOfWork.WorkShifts.GetById(shift.Id);
            if (entity != null)
            {
                entity.Name = shift.Name;
                entity.StartTime = shift.StartTime;
                entity.EndTime = shift.EndTime;
            }
            _unitOfWork.Complete();
        }

        public void DeleteWorkShift(int id)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            // Kiểm tra xem ca làm việc có đang được sử dụng trong lịch tương lai không
            var today = DateTime.Today;
            var isUsed = _unitOfWork.EmployeeSchedules.Find(s => s.WorkShiftId == id && s.ScheduleDate >= today).Any();
            if (isUsed)
            {
                throw new Exception("Không thể xóa ca này. Ca đang được sử dụng trong lịch làm việc từ hôm nay trở đi.");
            }

            var shift = _unitOfWork.WorkShifts.GetById(id);
            if (shift != null)
            {
                _unitOfWork.WorkShifts.Remove(shift);
                _unitOfWork.Complete();
            }
        }

        public List<EmployeeSchedule> GetSchedulesByDateRange(DateTime startDate, DateTime endDate)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            // Trả về kèm Employee và WorkShift để hiển thị
            return _unitOfWork.EmployeeSchedules.Find(s => s.ScheduleDate >= startDate && s.ScheduleDate <= endDate).ToList();
            // Note: EF lazy loading or explicit includes might be needed depending on UI usage.
        }

        public void SaveWeeklySchedules(List<EmployeeSchedule> schedules, DateTime weekStart, DateTime weekEnd)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            // Xóa tất cả các lịch trong tuần này trước khi lưu
            var existingSchedules = _unitOfWork.EmployeeSchedules.Find(s => s.ScheduleDate >= weekStart && s.ScheduleDate <= weekEnd).ToList();
            if (existingSchedules.Any())
            {
                _unitOfWork.EmployeeSchedules.RemoveRange(existingSchedules);
            }

            if (schedules != null && schedules.Any())
            {
                // Load tất cả nhân viên để lấy Role và ContractStatus
                var employees = _unitOfWork.Employees.GetAll().ToDictionary(e => e.Id, e => e);
                var shifts = _unitOfWork.WorkShifts.GetAll().ToDictionary(s => s.Id, s => s);

                // Group by (Date, ShiftId)
                var groupedSchedules = schedules.GroupBy(s => new { s.ScheduleDate, s.WorkShiftId });

                foreach (var group in groupedSchedules)
                {
                    int dentistCount = 0;
                    int receptionistCount = 0;

                    foreach (var schedule in group)
                    {
                        if (schedule.ShiftCoefficient.HasValue && (schedule.ShiftCoefficient.Value < 1.0f || schedule.ShiftCoefficient.Value > 1.5f))
                        {
                            throw new Exception("Hệ số ca làm việc phải từ 1.0 đến 1.5.");
                        }

                        if (schedule.PatientCoefficient < 0f)
                        {
                            throw new Exception("Hệ số bệnh nhân không được âm.");
                        }

                        if (employees.TryGetValue(schedule.EmployeeId, out var emp))
                        {
                            if (emp.ContractStatus == ContractStatus.Resigned && schedule.ScheduleDate >= DateTime.Today)
                            {
                                throw new Exception($"Nhân viên {emp.FullName} đã nghỉ việc, không được xếp lịch vào ngày {schedule.ScheduleDate:dd/MM/yyyy}.");
                            }

                            if (emp.Role == EmployeeRole.Dentist) dentistCount++;
                            if (emp.Role == EmployeeRole.Receptionist) receptionistCount++;
                        }
                    }

                    if (dentistCount < 4 || receptionistCount < 1)
                    {
                        var shiftName = shifts.ContainsKey(group.Key.WorkShiftId) ? shifts[group.Key.WorkShiftId].Name : $"Ca {group.Key.WorkShiftId}";
                        throw new Exception($"Không thể lưu lịch. Ca làm việc '{shiftName}' ngày {group.Key.ScheduleDate:dd/MM/yyyy} phải có ít nhất 1 Lễ tân và 4 Nha sĩ.");
                    }
                }

                foreach (var schedule in schedules)
                {
                    _unitOfWork.EmployeeSchedules.Add(schedule);
                }
            }

            _unitOfWork.Complete();
        }

        public void ClonePreviousWeekSchedules(DateTime targetWeekStart)
        {
            UserContext.CheckRole(EmployeeRole.Manager);

            var targetWeekEnd = targetWeekStart.AddDays(6);
            var prevWeekStart = targetWeekStart.AddDays(-7);
            var prevWeekEnd = prevWeekStart.AddDays(6);

            var prevSchedules = _unitOfWork.EmployeeSchedules.Find(s => s.ScheduleDate >= prevWeekStart && s.ScheduleDate <= prevWeekEnd).ToList();

            if (!prevSchedules.Any())
            {
                throw new Exception("Không thể xếp lịch tự động. Tuần trước chưa có lịch làm việc.");
            }

            var clonedSchedules = new List<EmployeeSchedule>();
            foreach (var prev in prevSchedules)
            {
                var newSchedule = new EmployeeSchedule
                {
                    EmployeeId = prev.EmployeeId,
                    WorkShiftId = prev.WorkShiftId,
                    ScheduleDate = prev.ScheduleDate.AddDays(7), // Shift by 7 days
                    ShiftCoefficient = prev.ShiftCoefficient,
                    PatientCoefficient = prev.PatientCoefficient
                };
                clonedSchedules.Add(newSchedule);
            }

            SaveWeeklySchedules(clonedSchedules, targetWeekStart, targetWeekEnd);
        }
    }
}
