using System;
using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Interfaces
{
    public interface IAppointmentService
    {
        IEnumerable<Appointment> GetAllAppointments();
        IEnumerable<Appointment> GetAppointmentsByDate(DateTime date);
        void AddAppointment(Appointment appointment);
        void UpdateAppointment(Appointment appointment);
        void CancelAppointment(int id);
    }
}
