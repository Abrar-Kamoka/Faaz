using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Student.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SrNoAppManaged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RemoveIdentity(migrationBuilder, "StudentProfiles");
            RemoveIdentity(migrationBuilder, "StudentSixthFormData");
            RemoveIdentity(migrationBuilder, "StudentUndergraduateData");
            RemoveIdentity(migrationBuilder, "StudentPostgraduateData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreIdentity(migrationBuilder, "StudentProfiles");
            RestoreIdentity(migrationBuilder, "StudentSixthFormData");
            RestoreIdentity(migrationBuilder, "StudentUndergraduateData");
            RestoreIdentity(migrationBuilder, "StudentPostgraduateData");
        }

        private static void RemoveIdentity(MigrationBuilder b, string table)
        {
            b.Sql($"ALTER TABLE [{table}] ADD [SrNo_new] INT NOT NULL DEFAULT 0");
            b.Sql($"UPDATE [{table}] SET [SrNo_new] = [SrNo]");
            b.Sql($"ALTER TABLE [{table}] DROP COLUMN [SrNo]");
            b.Sql($"EXEC sp_rename N'[{table}].[SrNo_new]', N'SrNo', N'COLUMN'");
            b.Sql($@"
                DECLARE @df NVARCHAR(256);
                SELECT @df = dc.name
                FROM sys.default_constraints dc
                JOIN sys.columns c
                    ON dc.parent_column_id = c.column_id
                   AND dc.parent_object_id = c.object_id
                WHERE OBJECT_NAME(dc.parent_object_id) = '{table}'
                  AND c.name = 'SrNo';
                IF @df IS NOT NULL
                    EXEC('ALTER TABLE [{table}] DROP CONSTRAINT [' + @df + ']');");
        }

        private static void RestoreIdentity(MigrationBuilder b, string table)
        {
            b.Sql($"ALTER TABLE [{table}] DROP COLUMN [SrNo]");
            b.Sql($"ALTER TABLE [{table}] ADD [SrNo] INT IDENTITY(1,1) NOT NULL");
        }
    }
}
