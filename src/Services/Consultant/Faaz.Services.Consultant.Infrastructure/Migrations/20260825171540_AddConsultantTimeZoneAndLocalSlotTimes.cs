using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantTimeZoneAndLocalSlotTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTimeUtc",
                schema: "consultant",
                table: "ConsultantAvailabilitySlots",
                newName: "StartTimeLocal");

            migrationBuilder.RenameColumn(
                name: "EndTimeUtc",
                schema: "consultant",
                table: "ConsultantAvailabilitySlots",
                newName: "EndTimeLocal");

            migrationBuilder.AlterColumn<string>(
                name: "StripeAccountId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1022)
                .OldAnnotation("Relational:ColumnOrder", 1021);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeDetailsSubmitted",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1023)
                .OldAnnotation("Relational:ColumnOrder", 1022);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeChargesEnabled",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1024)
                .OldAnnotation("Relational:ColumnOrder", 1023);

            migrationBuilder.AlterColumn<bool>(
                name: "IsProfileComplete",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1019)
                .OldAnnotation("Relational:ColumnOrder", 1018);

            migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false)
                .Annotation("Relational:ColumnOrder", 1021)
                .OldAnnotation("Relational:ColumnOrder", 1020);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1020)
                .OldAnnotation("Relational:ColumnOrder", 1019);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "UTC")
                .Annotation("Relational:ColumnOrder", 1018);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                schema: "consultant",
                table: "ConsultantProfiles");

            migrationBuilder.RenameColumn(
                name: "StartTimeLocal",
                schema: "consultant",
                table: "ConsultantAvailabilitySlots",
                newName: "StartTimeUtc");

            migrationBuilder.RenameColumn(
                name: "EndTimeLocal",
                schema: "consultant",
                table: "ConsultantAvailabilitySlots",
                newName: "EndTimeUtc");

            migrationBuilder.AlterColumn<string>(
                name: "StripeAccountId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1021)
                .OldAnnotation("Relational:ColumnOrder", 1022);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeDetailsSubmitted",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1022)
                .OldAnnotation("Relational:ColumnOrder", 1023);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeChargesEnabled",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1023)
                .OldAnnotation("Relational:ColumnOrder", 1024);

            migrationBuilder.AlterColumn<bool>(
                name: "IsProfileComplete",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1018)
                .OldAnnotation("Relational:ColumnOrder", 1019);

            migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false)
                .Annotation("Relational:ColumnOrder", 1020)
                .OldAnnotation("Relational:ColumnOrder", 1021);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1019)
                .OldAnnotation("Relational:ColumnOrder", 1020);
        }
    }
}
