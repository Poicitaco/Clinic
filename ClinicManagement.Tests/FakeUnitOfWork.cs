using System;
using ClinicManagement.Core;
using ClinicManagement.DataAccess.Repositories;
using ClinicManagement.DataAccess.UnitOfWork;

namespace ClinicManagement.Tests
{
    public class FakeUnitOfWork : IUnitOfWork
    {
        public IRepository<Employee> Employees { get; } = new FakeRepository<Employee>();
        public IRepository<Account> Accounts { get; } = new FakeRepository<Account>();
        public IRepository<Patient> Patients { get; } = new FakeRepository<Patient>();
        public IRepository<ServiceCategory> ServiceCategories { get; } = new FakeRepository<ServiceCategory>();
        public IRepository<Service> Services { get; } = new FakeRepository<Service>();
        public IRepository<Appointment> Appointments { get; } = new FakeRepository<Appointment>();
        public IRepository<PatientExamination> PatientExaminations { get; } = new FakeRepository<PatientExamination>();
        public IRepository<Invoice> Invoices { get; } = new FakeRepository<Invoice>();
        public IRepository<InvoiceDetail> InvoiceDetails { get; } = new FakeRepository<InvoiceDetail>();
        public IRepository<InvoicePayment> InvoicePayments { get; } = new FakeRepository<InvoicePayment>();
        public IRepository<WorkShift> WorkShifts { get; } = new FakeRepository<WorkShift>();
        public IRepository<EmployeeSchedule> EmployeeSchedules { get; } = new FakeRepository<EmployeeSchedule>();
        public IRepository<SalaryConfiguration> SalaryConfigurations { get; } = new FakeRepository<SalaryConfiguration>();
        public IRepository<DentistDegreeSalaryCoefficient> DentistDegreeSalaryCoefficients { get; } = new FakeRepository<DentistDegreeSalaryCoefficient>();
        public IRepository<SalaryRecord> SalaryRecords { get; } = new FakeRepository<SalaryRecord>();
        public IRepository<SalaryFormulaSnapshot> SalaryFormulaSnapshots { get; } = new FakeRepository<SalaryFormulaSnapshot>();
        public IRepository<ServiceStage> ServiceStages { get; } = new FakeRepository<ServiceStage>();
        public IRepository<ServicePriceHistory> ServicePriceHistories { get; } = new FakeRepository<ServicePriceHistory>();

        public int Complete() => 1;
        public void Save() {}
        public void Dispose() {}
    }
}
