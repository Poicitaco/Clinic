using System;
using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Interfaces
{
    public interface IWorkShiftService
    {
        // Quản lý ca làm việc
        List<WorkShift> GetAllWorkShifts();
        WorkShift GetWorkShiftById(int id);
        void AddWorkShift(WorkShift shift);
        void UpdateWorkShift(WorkShift shift);
        void DeleteWorkShift(int id);

        // Quản lý xếp lịch
        List<EmployeeSchedule> GetSchedulesByDateRange(DateTime startDate, DateTime endDate);
        void SaveWeeklySchedules(List<EmployeeSchedule> schedules, DateTime weekStart, DateTime weekEnd);
        void ClonePreviousWeekSchedules(DateTime targetWeekStart);
    }
}
