using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InAppNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InAppNotificationsEnabled",
                table: "Users");
        }
    }
}
