namespace ClinicManagement.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPasswordResetFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "PasswordResetTokenHash", c => c.String(unicode: false));
            AddColumn("dbo.Accounts", "PasswordResetTokenExpiresAt", c => c.DateTime(precision: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "PasswordResetTokenExpiresAt");
            DropColumn("dbo.Accounts", "PasswordResetTokenHash");
        }
    }
}
