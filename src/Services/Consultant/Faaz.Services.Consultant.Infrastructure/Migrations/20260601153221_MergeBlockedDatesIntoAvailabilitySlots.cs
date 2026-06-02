using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeBlockedDatesIntoAvailabilitySlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultantBlockedDates");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTimeUtc",
                table: "ConsultantAvailabilitySlots",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "EndTimeUtc",
                table: "ConsultantAvailabilitySlots",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "ConsultantAvailabilitySlots",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ConsultantAvailabilitySlots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlockedDate",
                table: "ConsultantAvailabilitySlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ConsultantAvailabilitySlots",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "ConsultantAvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "IsBlockedDate",
                table: "ConsultantAvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ConsultantAvailabilitySlots");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTimeUtc",
                table: "ConsultantAvailabilitySlots",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "EndTimeUtc",
                table: "ConsultantAvailabilitySlots",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "ConsultantAvailabilitySlots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

        }
    }
}
