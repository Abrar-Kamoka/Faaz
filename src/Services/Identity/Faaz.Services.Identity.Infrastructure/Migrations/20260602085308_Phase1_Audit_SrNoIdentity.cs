using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Audit_SrNoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the obsolete Status columns (BaseEntity.Status bool? removed).
            migrationBuilder.DropColumn(name: "Status", table: "RefreshTokens");
            migrationBuilder.DropColumn(name: "Status", table: "PasswordResetTokens");

            // SQL Server cannot ALTER COLUMN to add IDENTITY — must drop and recreate.
            // Existing NULL values are discarded (SrNo was never reliably populated before).
            migrationBuilder.DropColumn(name: "SrNo", table: "Users");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "Users", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "RefreshTokens");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "RefreshTokens", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "PasswordResetTokens");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "PasswordResetTokens", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert IDENTITY columns back to plain nullable int.
            migrationBuilder.DropColumn(name: "SrNo", table: "Users");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "Users", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "RefreshTokens");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "RefreshTokens", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "PasswordResetTokens");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "PasswordResetTokens", type: "int", nullable: true);

            // Restore dropped Status columns.
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "RefreshTokens", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "PasswordResetTokens", type: "bit", nullable: true);
        }
    }
}
