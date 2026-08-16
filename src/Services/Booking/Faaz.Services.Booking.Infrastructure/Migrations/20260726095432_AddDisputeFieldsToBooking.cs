using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeFieldsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                schema: "booking",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeResolution",
                schema: "booking",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeResolutionNote",
                schema: "booking",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeResolvedAt",
                schema: "booking",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeReason",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DisputeResolution",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DisputeResolutionNote",
                schema: "booking",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DisputeResolvedAt",
                schema: "booking",
                table: "Bookings");
        }
    }
}
