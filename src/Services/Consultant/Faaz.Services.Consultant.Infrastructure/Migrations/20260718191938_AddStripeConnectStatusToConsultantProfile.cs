using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnectStatusToConsultantProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStripeChargesEnabled",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStripeDetailsSubmitted",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStripeChargesEnabled",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "IsStripeDetailsSubmitted",
                table: "ConsultantProfiles");
        }
    }
}
