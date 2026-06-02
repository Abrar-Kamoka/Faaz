using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Audit_SrNoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the obsolete Status columns (BaseEntity.Status bool? removed).
            migrationBuilder.DropColumn(name: "Status", table: "ConsultantSessionTypes");
            migrationBuilder.DropColumn(name: "Status", table: "ConsultantProfiles");
            migrationBuilder.DropColumn(name: "Status", table: "ConsultantAvailabilitySlots");
            migrationBuilder.DropColumn(name: "Status", table: "ConsultantApplications");

            // SQL Server cannot ALTER COLUMN to add IDENTITY — must drop and recreate.
            // Existing NULL values are discarded (SrNo was never reliably populated before).
            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantSessionTypes");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantSessionTypes", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantProfiles");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantProfiles", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantAvailabilitySlots");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantAvailabilitySlots", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantApplications");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantApplications", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // New filtered index on invite token — required for account creation performance.
            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplications_SetupInviteToken",
                table: "ConsultantApplications",
                column: "SetupInviteToken",
                filter: "[SetupInviteToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConsultantApplications_SetupInviteToken",
                table: "ConsultantApplications");

            // Revert IDENTITY columns back to plain nullable int.
            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantSessionTypes");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantSessionTypes", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantProfiles");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantProfiles", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantAvailabilitySlots");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantAvailabilitySlots", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "ConsultantApplications");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "ConsultantApplications", type: "int", nullable: true);

            // Restore dropped Status columns.
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "ConsultantSessionTypes", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "ConsultantProfiles", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "ConsultantAvailabilitySlots", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "ConsultantApplications", type: "bit", nullable: true);
        }
    }
}
