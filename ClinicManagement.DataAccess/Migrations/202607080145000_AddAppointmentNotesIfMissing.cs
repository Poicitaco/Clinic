namespace ClinicManagement.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddAppointmentNotesIfMissing : DbMigration
    {
        public override void Up()
        {
            Sql(@"
                SET @schema_name = DATABASE();
                SET @column_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'appointments'
                      AND COLUMN_NAME = 'Notes'
                );
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `appointments` ADD COLUMN `Notes` longtext NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        public override void Down()
        {
            Sql(@"
                SET @schema_name = DATABASE();
                SET @column_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema_name
                      AND TABLE_NAME = 'appointments'
                      AND COLUMN_NAME = 'Notes'
                );
                SET @sql = IF(@column_exists = 1,
                    'ALTER TABLE `appointments` DROP COLUMN `Notes`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
