using ClinicManagement.Core;
using ClinicManagement.DataAccess.Repositories;
using System;

namespace ClinicManagement.DataAccess.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Employee> Employees { get; }
        IRepository<Account> Accounts { get; }
        IRepository<Patient> Patients { get; }
        IRepository<ServiceCategory> ServiceCategories { get; }
        IRepository<Service> Services { get; }
        IRepository<Appointment> Appointments { get; }
        IRepository<PatientExamination> PatientExaminations { get; }
        IRepository<Invoice> Invoices { get; }
        IRepository<InvoiceDetail> InvoiceDetails { get; }
        IRepository<InvoicePayment> InvoicePayments { get; }
        IRepository<WorkShift> WorkShifts { get; }
        IRepository<EmployeeSchedule> EmployeeSchedules { get; }
        IRepository<SalaryConfiguration> SalaryConfigurations { get; }
        IRepository<DentistDegreeSalaryCoefficient> DentistDegreeSalaryCoefficients { get; }
        IRepository<SalaryRecord> SalaryRecords { get; }
        IRepository<SalaryFormulaSnapshot> SalaryFormulaSnapshots { get; }
        IRepository<ServiceStage> ServiceStages { get; }
        IRepository<ServicePriceHistory> ServicePriceHistories { get; }

        void Save();
        int Complete();
    }
}
