namespace ClinicManagement.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RedesignSalaryConfiguration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DentistDegreeSalaryCoefficients",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SalaryConfigurationId = c.Int(nullable: false),
                        Degree = c.Int(nullable: false),
                        Coefficient = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SalaryConfigurations", t => t.SalaryConfigurationId)
                .Index(t => new { t.SalaryConfigurationId, t.Degree }, unique: true, name: "IX_DentistDegreeSalaryCoefficient_Config_Degree");
            
            AddColumn("dbo.SalaryConfigurations", "DefaultShiftCoefficient", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.SalaryConfigurations", "ReceptionistCoefficient", c => c.Decimal(nullable: false, precision: 18, scale: 2));

            // Preserve one global salary configuration from the old role-based rows.
            // Old Role values: Dentist = 1, Receptionist = 2.
            Sql(@"
INSERT INTO `SalaryConfigurations` (`Role`, `BaseSalary`, `HourlyRate`, `Allowance`, `DefaultShiftCoefficient`, `ReceptionistCoefficient`)
SELECT 1, 0, 1000, 0, 1.0, 1.0
WHERE NOT EXISTS (SELECT 1 FROM `SalaryConfigurations`);
");
            Sql(@"
UPDATE `SalaryConfigurations` AS salaryConfig
JOIN (
    SELECT
        MIN(`Id`) AS KeepId,
        COALESCE(
            (SELECT `HourlyRate`
             FROM (SELECT `HourlyRate` FROM `SalaryConfigurations` WHERE `Role` = 1 ORDER BY `Id` LIMIT 1) AS dentistConfig),
            (SELECT `HourlyRate`
             FROM (SELECT `HourlyRate` FROM `SalaryConfigurations` ORDER BY `Id` LIMIT 1) AS firstConfig),
            1000
        ) AS GlobalHourlyRate
    FROM `SalaryConfigurations`
) AS configSource ON salaryConfig.`Id` = configSource.KeepId
SET
    salaryConfig.`HourlyRate` = configSource.GlobalHourlyRate,
    salaryConfig.`DefaultShiftCoefficient` = 1.0,
    salaryConfig.`ReceptionistCoefficient` = 1.0;
");
            Sql(@"
DELETE FROM `SalaryConfigurations`
WHERE `Id` NOT IN (
    SELECT KeepId
    FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep
);
");
            Sql(@"
INSERT INTO `DentistDegreeSalaryCoefficients` (`SalaryConfigurationId`, `Degree`, `Coefficient`)
SELECT KeepId, 1, 1.0 FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep UNION ALL
SELECT KeepId, 2, 1.0 FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep UNION ALL
SELECT KeepId, 3, 1.0 FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep UNION ALL
SELECT KeepId, 4, 1.0 FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep UNION ALL
SELECT KeepId, 5, 1.0 FROM (SELECT MIN(`Id`) AS KeepId FROM `SalaryConfigurations`) AS configToKeep;
");

            DropColumn("dbo.SalaryConfigurations", "Role");
            DropColumn("dbo.SalaryConfigurations", "BaseSalary");
            DropColumn("dbo.SalaryConfigurations", "Allowance");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SalaryConfigurations", "Allowance", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.SalaryConfigurations", "BaseSalary", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.SalaryConfigurations", "Role", c => c.Int(nullable: false));
            Sql(@"UPDATE `SalaryConfigurations` SET `Role` = 1, `BaseSalary` = 0, `Allowance` = 0;");
            DropForeignKey("dbo.DentistDegreeSalaryCoefficients", "SalaryConfigurationId", "dbo.SalaryConfigurations");
            DropIndex("dbo.DentistDegreeSalaryCoefficients", "IX_DentistDegreeSalaryCoefficient_Config_Degree");
            DropColumn("dbo.SalaryConfigurations", "ReceptionistCoefficient");
            DropColumn("dbo.SalaryConfigurations", "DefaultShiftCoefficient");
            DropTable("dbo.DentistDegreeSalaryCoefficients");
        }
    }
}
