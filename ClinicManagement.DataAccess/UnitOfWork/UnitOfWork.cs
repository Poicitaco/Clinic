using ClinicManagement.Core;
using ClinicManagement.DataAccess.Repositories;

namespace ClinicManagement.DataAccess.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ClinicDbContext _context;

        public IRepository<Employee> Employees { get; private set; }
        public IRepository<Account> Accounts { get; private set; }
        public IRepository<Patient> Patients { get; private set; }
        public IRepository<ServiceCategory> ServiceCategories { get; private set; }
        public IRepository<Service> Services { get; private set; }
        public IRepository<Appointment> Appointments { get; private set; }
        public IRepository<PatientExamination> PatientExaminations { get; private set; }
        public IRepository<Invoice> Invoices { get; private set; }
        public IRepository<InvoiceDetail> InvoiceDetails { get; private set; }
        public IRepository<InvoicePayment> InvoicePayments { get; private set; }
        public IRepository<WorkShift> WorkShifts { get; private set; }
        public IRepository<EmployeeSchedule> EmployeeSchedules { get; private set; }
        public IRepository<SalaryConfiguration> SalaryConfigurations { get; private set; }
        public IRepository<DentistDegreeSalaryCoefficient> DentistDegreeSalaryCoefficients { get; private set; }
        public IRepository<SalaryRecord> SalaryRecords { get; private set; }
        public IRepository<SalaryFormulaSnapshot> SalaryFormulaSnapshots { get; private set; }
        public IRepository<ServiceStage> ServiceStages { get; private set; }
        public IRepository<ServicePriceHistory> ServicePriceHistories { get; private set; }

        public UnitOfWork(ClinicDbContext context)
        {
            _context = context;
            Employees = new Repository<Employee>(_context);
            Accounts = new Repository<Account>(_context);
            Patients = new Repository<Patient>(_context);
            ServiceCategories = new Repository<ServiceCategory>(_context);
            Services = new Repository<Service>(_context);
            Appointments = new Repository<Appointment>(_context);
            PatientExaminations = new Repository<PatientExamination>(_context);
            Invoices = new Repository<Invoice>(_context);
            InvoiceDetails = new Repository<InvoiceDetail>(_context);
            InvoicePayments = new Repository<InvoicePayment>(_context);
            WorkShifts = new Repository<WorkShift>(_context);
            EmployeeSchedules = new Repository<EmployeeSchedule>(_context);
            SalaryConfigurations = new Repository<SalaryConfiguration>(_context);
            DentistDegreeSalaryCoefficients = new Repository<DentistDegreeSalaryCoefficient>(_context);
            SalaryRecords = new Repository<SalaryRecord>(_context);
            SalaryFormulaSnapshots = new Repository<SalaryFormulaSnapshot>(_context);
            ServiceStages = new Repository<ServiceStage>(_context);
            ServicePriceHistories = new Repository<ServicePriceHistory>(_context);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
