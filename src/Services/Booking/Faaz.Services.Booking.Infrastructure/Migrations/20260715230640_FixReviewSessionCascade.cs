using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewSessionCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Sessions_SessionId",
                schema: "booking",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Sessions_SessionId",
                schema: "booking",
                table: "Reviews",
                column: "SessionId",
                principalSchema: "booking",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Sessions_SessionId",
                schema: "booking",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Sessions_SessionId",
                schema: "booking",
                table: "Reviews",
                column: "SessionId",
                principalSchema: "booking",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
