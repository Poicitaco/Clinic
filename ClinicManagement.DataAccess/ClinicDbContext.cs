using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using ClinicManagement.Core;

namespace ClinicManagement.DataAccess
{
    public class ClinicDbContext : DbContext
    {
        static ClinicDbContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ClinicDbContext, ClinicManagement.DataAccess.Migrations.Configuration>());
        }

        public ClinicDbContext() : base("name=ClinicDbContext")
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<PatientExamination> PatientExaminations { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        public DbSet<SalaryConfiguration> SalaryConfigurations { get; set; }
        public DbSet<DentistDegreeSalaryCoefficient> DentistDegreeSalaryCoefficients { get; set; }
        public DbSet<SalaryRecord> SalaryRecords { get; set; }
        public DbSet<SalaryFormulaSnapshot> SalaryFormulaSnapshots { get; set; }
        public DbSet<ServiceStage> ServiceStages { get; set; }
        public DbSet<ServicePriceHistory> ServicePriceHistories { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SalaryRecord>()
                .HasOptional(r => r.FormulaSnapshot)
                .WithRequired(s => s.SalaryRecord)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DentistDegreeSalaryCoefficient>()
                .HasRequired(c => c.SalaryConfiguration)
                .WithMany(c => c.DentistDegreeCoefficients)
                .HasForeignKey(c => c.SalaryConfigurationId);

            modelBuilder.Entity<DentistDegreeSalaryCoefficient>()
                .Property(c => c.Degree)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_DentistDegreeSalaryCoefficient_Config_Degree", 2) { IsUnique = true }));

            modelBuilder.Entity<DentistDegreeSalaryCoefficient>()
                .Property(c => c.SalaryConfigurationId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_DentistDegreeSalaryCoefficient_Config_Degree", 1) { IsUnique = true }));

            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.OneToManyCascadeDeleteConvention>();

            // Employee configurations
            modelBuilder.Entity<Employee>()
                .Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName, 
                    new IndexAnnotation(new IndexAttribute("IX_Employee_Email") { IsUnique = true }));

            modelBuilder.Entity<Employee>()
                .Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName, 
                    new IndexAnnotation(new IndexAttribute("IX_Employee_PhoneNumber") { IsUnique = true }));

            // Service
            modelBuilder.Entity<Service>()
                .HasRequired(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId);

            // ServiceStage
            modelBuilder.Entity<ServiceStage>()
                .HasRequired(s => s.Service)
                .WithMany(s => s.Stages)
                .HasForeignKey(s => s.ServiceId);

            // Account configurations
            modelBuilder.Entity<Account>()
                .HasRequired(a => a.Employee)
                .WithOptional(e => e.Account);

            modelBuilder.Entity<Account>()
                .Property(a => a.Username)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName, 
                    new IndexAnnotation(new IndexAttribute("IX_Account_Username") { IsUnique = true }));
        }
    }
}
