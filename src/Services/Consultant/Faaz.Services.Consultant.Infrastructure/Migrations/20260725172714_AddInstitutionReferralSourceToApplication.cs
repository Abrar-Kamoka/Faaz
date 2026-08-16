using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionReferralSourceToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExpertiseArea",
                table: "ConsultantApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                table: "ConsultantApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Institution",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "ReferralSource",
                table: "ConsultantApplications");

            migrationBuilder.AlterColumn<string>(
                name: "ExpertiseArea",
                table: "ConsultantApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);
        }
    }
}
