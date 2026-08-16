using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFeaturedToConsultantProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "ConsultantProfiles");
        }
    }
}
