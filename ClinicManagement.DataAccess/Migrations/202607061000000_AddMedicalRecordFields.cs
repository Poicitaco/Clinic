namespace ClinicManagement.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMedicalRecordFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PatientExaminations", "ProposedServices", c => c.String(unicode: false));
            AddColumn("dbo.PatientExaminations", "ReExamDate", c => c.DateTime(precision: 0));
            AddColumn("dbo.PatientExaminations", "ManagerInterventionReason", c => c.String(unicode: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PatientExaminations", "ManagerInterventionReason");
            DropColumn("dbo.PatientExaminations", "ReExamDate");
            DropColumn("dbo.PatientExaminations", "ProposedServices");
        }
    }
}
