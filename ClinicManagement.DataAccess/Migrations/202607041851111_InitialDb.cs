namespace ClinicManagement.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InitialDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Accounts",
                c => new
                {
                    Id = c.Int(nullable: false),
                    Username = c.String(nullable: false, maxLength: 255, storeType: "nvarchar"),
                    PasswordHash = c.String(unicode: false),
                    IsActive = c.Boolean(nullable: false),
                    EmployeeId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employees", t => t.Id)
                .Index(t => t.Id)
                .Index(t => t.Username, unique: true, name: "IX_Account_Username");

            CreateTable(
                "dbo.Employees",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    FullName = c.String(nullable: false, unicode: false),
                    DateOfBirth = c.DateTime(nullable: false, precision: 0),
                    Gender = c.Int(nullable: false),
                    PhoneNumber = c.String(nullable: false, maxLength: 10, storeType: "nvarchar"),
                    Email = c.String(nullable: false, maxLength: 255, storeType: "nvarchar"),
                    Address = c.String(maxLength: 255, storeType: "nvarchar"),
                    AvatarUrl = c.String(unicode: false),
                    Role = c.Int(nullable: false),
                    StartDate = c.DateTime(nullable: false, precision: 0),
                    ContractStatus = c.Int(nullable: false),
                    Degree = c.Int(),
                    ResignationDate = c.DateTime(precision: 0),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.PhoneNumber, unique: true, name: "IX_Employee_PhoneNumber")
                .Index(t => t.Email, unique: true, name: "IX_Employee_Email");

            CreateTable(
                "dbo.Appointments",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    PatientName = c.String(nullable: false, unicode: false),
                    PhoneNumber = c.String(nullable: false, unicode: false),
                    DentistId = c.Int(nullable: false),
                    StartTime = c.DateTime(nullable: false, precision: 0),
                    EndTime = c.DateTime(nullable: false, precision: 0),
                    Status = c.Int(nullable: false),
                    CreatedById = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employees", t => t.CreatedById)
                .ForeignKey("dbo.Employees", t => t.DentistId)
                .Index(t => t.DentistId)
                .Index(t => t.CreatedById);

            CreateTable(
                "dbo.InvoiceDetails",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    InvoiceId = c.Int(nullable: false),
                    ServiceId = c.Int(nullable: false),
                    Quantity = c.Int(nullable: false),
                    UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Invoices", t => t.InvoiceId)
                .ForeignKey("dbo.Services", t => t.ServiceId)
                .Index(t => t.InvoiceId)
                .Index(t => t.ServiceId);

            CreateTable(
                "dbo.Invoices",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    PatientId = c.Int(nullable: false),
                    ExaminationId = c.Int(),
                    CreatedDate = c.DateTime(nullable: false, precision: 0),
                    CreatedById = c.Int(nullable: false),
                    Notes = c.String(unicode: false),
                    TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Status = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employees", t => t.CreatedById)
                .ForeignKey("dbo.PatientExaminations", t => t.ExaminationId)
                .ForeignKey("dbo.Patients", t => t.PatientId)
                .Index(t => t.PatientId)
                .Index(t => t.ExaminationId)
                .Index(t => t.CreatedById);

            CreateTable(
                "dbo.PatientExaminations",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    PatientId = c.Int(nullable: false),
                    AppointmentId = c.Int(),
                    DentistId = c.Int(nullable: false),
                    ExaminationDate = c.DateTime(nullable: false, precision: 0),
                    Symptoms = c.String(unicode: false),
                    Diagnosis = c.String(unicode: false),
                    TreatmentPlan = c.String(unicode: false),
                    Prescription = c.String(unicode: false),
                    Notes = c.String(unicode: false),
                    Status = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Appointments", t => t.AppointmentId)
                .ForeignKey("dbo.Employees", t => t.DentistId)
                .ForeignKey("dbo.Patients", t => t.PatientId)
                .Index(t => t.PatientId)
                .Index(t => t.AppointmentId)
                .Index(t => t.DentistId);

            CreateTable(
                "dbo.Patients",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    FullName = c.String(nullable: false, unicode: false),
                    DateOfBirth = c.DateTime(nullable: false, precision: 0),
                    Gender = c.Int(nullable: false),
                    PhoneNumber = c.String(nullable: false, maxLength: 10, storeType: "nvarchar"),
                    Email = c.String(unicode: false),
                    Address = c.String(maxLength: 255, storeType: "nvarchar"),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.Services",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, unicode: false),
                    Description = c.String(unicode: false),
                    CategoryId = c.Int(nullable: false),
                    Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                    IsActive = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ServiceCategories", t => t.CategoryId)
                .Index(t => t.CategoryId);

            CreateTable(
                "dbo.ServiceCategories",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, unicode: false),
                    Description = c.String(unicode: false),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.InvoiceDetails", "ServiceId", "dbo.Services");
            DropForeignKey("dbo.Services", "CategoryId", "dbo.ServiceCategories");
            DropForeignKey("dbo.Invoices", "PatientId", "dbo.Patients");
            DropForeignKey("dbo.InvoiceDetails", "InvoiceId", "dbo.Invoices");
            DropForeignKey("dbo.Invoices", "ExaminationId", "dbo.PatientExaminations");
            DropForeignKey("dbo.PatientExaminations", "PatientId", "dbo.Patients");
            DropForeignKey("dbo.PatientExaminations", "DentistId", "dbo.Employees");
            DropForeignKey("dbo.PatientExaminations", "AppointmentId", "dbo.Appointments");
            DropForeignKey("dbo.Invoices", "CreatedById", "dbo.Employees");
            DropForeignKey("dbo.Appointments", "DentistId", "dbo.Employees");
            DropForeignKey("dbo.Appointments", "CreatedById", "dbo.Employees");
            DropForeignKey("dbo.Accounts", "Id", "dbo.Employees");
            DropIndex("dbo.Services", new[] { "CategoryId" });
            DropIndex("dbo.PatientExaminations", new[] { "DentistId" });
            DropIndex("dbo.PatientExaminations", new[] { "AppointmentId" });
            DropIndex("dbo.PatientExaminations", new[] { "PatientId" });
            DropIndex("dbo.Invoices", new[] { "CreatedById" });
            DropIndex("dbo.Invoices", new[] { "ExaminationId" });
            DropIndex("dbo.Invoices", new[] { "PatientId" });
            DropIndex("dbo.InvoiceDetails", new[] { "ServiceId" });
            DropIndex("dbo.InvoiceDetails", new[] { "InvoiceId" });
            DropIndex("dbo.Appointments", new[] { "CreatedById" });
            DropIndex("dbo.Appointments", new[] { "DentistId" });
            DropIndex("dbo.Employees", "IX_Employee_Email");
            DropIndex("dbo.Employees", "IX_Employee_PhoneNumber");
            DropIndex("dbo.Accounts", "IX_Account_Username");
            DropIndex("dbo.Accounts", new[] { "Id" });
            DropTable("dbo.ServiceCategories");
            DropTable("dbo.Services");
            DropTable("dbo.Patients");
            DropTable("dbo.PatientExaminations");
            DropTable("dbo.Invoices");
            DropTable("dbo.InvoiceDetails");
            DropTable("dbo.Appointments");
            DropTable("dbo.Employees");
            DropTable("dbo.Accounts");
        }
    }
}
