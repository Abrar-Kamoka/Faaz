using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SrNoAppManaged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot remove IDENTITY via ALTER COLUMN.
            // Must add a plain column, copy data, drop the identity column, then rename.
            RemoveIdentity(migrationBuilder, "Users");
            RemoveIdentity(migrationBuilder, "RefreshTokens");
            RemoveIdentity(migrationBuilder, "PasswordResetTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreIdentity(migrationBuilder, "Users");
            RestoreIdentity(migrationBuilder, "RefreshTokens");
            RestoreIdentity(migrationBuilder, "PasswordResetTokens");
        }

        private static void RemoveIdentity(MigrationBuilder b, string table)
        {
            // 1. Add plain int column alongside the identity one
            b.Sql($"ALTER TABLE [{table}] ADD [SrNo_new] INT NOT NULL DEFAULT 0");
            // 2. Copy existing values
            b.Sql($"UPDATE [{table}] SET [SrNo_new] = [SrNo]");
            // 3. Drop the identity column
            b.Sql($"ALTER TABLE [{table}] DROP COLUMN [SrNo]");
            // 4. Rename the plain column to SrNo
            b.Sql($"EXEC sp_rename N'[{table}].[SrNo_new]', N'SrNo', N'COLUMN'");
            // 5. Drop the default constraint that was auto-created for SrNo_new
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
            // Drop the plain SrNo column and replace with an IDENTITY column.
            // Original SrNo values are not preserved on rollback.
            b.Sql($"ALTER TABLE [{table}] DROP COLUMN [SrNo]");
            b.Sql($"ALTER TABLE [{table}] ADD [SrNo] INT IDENTITY(1,1) NOT NULL");
        }
    }
}
