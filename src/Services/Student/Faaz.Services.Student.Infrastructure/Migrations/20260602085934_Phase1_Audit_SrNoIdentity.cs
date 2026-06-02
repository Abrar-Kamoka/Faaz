using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Student.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Audit_SrNoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the obsolete Status columns (BaseEntity.Status bool? removed).
            migrationBuilder.DropColumn(name: "Status", table: "StudentUndergraduateData");
            migrationBuilder.DropColumn(name: "Status", table: "StudentSixthFormData");
            migrationBuilder.DropColumn(name: "Status", table: "StudentProfiles");
            migrationBuilder.DropColumn(name: "Status", table: "StudentPostgraduateData");

            // SQL Server cannot ALTER COLUMN to add IDENTITY — must drop and recreate.
            // Existing NULL values are discarded (SrNo was never reliably populated before).
            migrationBuilder.DropColumn(name: "SrNo", table: "StudentUndergraduateData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentUndergraduateData", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentSixthFormData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentSixthFormData", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentProfiles");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentProfiles", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentPostgraduateData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentPostgraduateData", type: "int", nullable: false, defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert IDENTITY columns back to plain nullable int.
            migrationBuilder.DropColumn(name: "SrNo", table: "StudentUndergraduateData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentUndergraduateData", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentSixthFormData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentSixthFormData", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentProfiles");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentProfiles", type: "int", nullable: true);

            migrationBuilder.DropColumn(name: "SrNo", table: "StudentPostgraduateData");
            migrationBuilder.AddColumn<int>(
                name: "SrNo", table: "StudentPostgraduateData", type: "int", nullable: true);

            // Restore dropped Status columns.
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "StudentUndergraduateData", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "StudentSixthFormData", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "StudentProfiles", type: "bit", nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Status", table: "StudentPostgraduateData", type: "bit", nullable: true);
        }
    }
}
