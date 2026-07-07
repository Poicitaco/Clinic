namespace ClinicManagement.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddAppointmentNotes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Appointments", "Notes", c => c.String(unicode: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Appointments", "Notes");
        }
    }
}
