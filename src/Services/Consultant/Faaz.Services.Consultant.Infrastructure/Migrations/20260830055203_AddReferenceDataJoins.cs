using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceDataJoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicesOffered",
                schema: "consultant",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "SpecialisedUniversities",
                schema: "consultant",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "SubjectAreas",
                schema: "consultant",
                table: "ConsultantProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "WrittenBio",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1010)
                .OldAnnotation("Relational:ColumnOrder", 1013);

            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)")
                .Annotation("Relational:ColumnOrder", 1015)
                .OldAnnotation("Relational:ColumnOrder", 1018);

            migrationBuilder.AlterColumn<string>(
                name: "StripeAccountId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1019)
                .OldAnnotation("Relational:ColumnOrder", 1022);

            migrationBuilder.AlterColumn<int>(
                name: "MinBookingNoticeHours",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1013)
                .OldAnnotation("Relational:ColumnOrder", 1016);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAdvanceBookingDays",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1014)
                .OldAnnotation("Relational:ColumnOrder", 1017);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeDetailsSubmitted",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1020)
                .OldAnnotation("Relational:ColumnOrder", 1023);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeChargesEnabled",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1021)
                .OldAnnotation("Relational:ColumnOrder", 1024);

            migrationBuilder.AlterColumn<bool>(
                name: "IsProfileComplete",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1016)
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
                .Annotation("Relational:ColumnOrder", 1018)
                .OldAnnotation("Relational:ColumnOrder", 1021);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1017)
                .OldAnnotation("Relational:ColumnOrder", 1020);

            migrationBuilder.AlterColumn<string>(
                name: "IntroVideoUrl",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1011)
                .OldAnnotation("Relational:ColumnOrder", 1014);

            migrationBuilder.AlterColumn<int>(
                name: "CallPreference",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1012)
                .OldAnnotation("Relational:ColumnOrder", 1015);

            migrationBuilder.CreateTable(
                name: "ConsultantProfileServices",
                schema: "consultant",
                columns: table => new
                {
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfileServices", x => new { x.ConsultantProfileId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ConsultantProfileServices_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantProfileSubjects",
                schema: "consultant",
                columns: table => new
                {
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfileSubjects", x => new { x.ConsultantProfileId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_ConsultantProfileSubjects_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantProfileUniversities",
                schema: "consultant",
                columns: table => new
                {
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByAdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfileUniversities", x => new { x.ConsultantProfileId, x.UniversityId });
                    table.ForeignKey(
                        name: "FK_ConsultantProfileUniversities_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultantProfileServices",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantProfileSubjects",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantProfileUniversities",
                schema: "consultant");

            migrationBuilder.AlterColumn<string>(
                name: "WrittenBio",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1013)
                .OldAnnotation("Relational:ColumnOrder", 1010);

            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)")
                .Annotation("Relational:ColumnOrder", 1018)
                .OldAnnotation("Relational:ColumnOrder", 1015);

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
                .OldAnnotation("Relational:ColumnOrder", 1019);

            migrationBuilder.AlterColumn<int>(
                name: "MinBookingNoticeHours",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1016)
                .OldAnnotation("Relational:ColumnOrder", 1013);

            migrationBuilder.AlterColumn<int>(
                name: "MaxAdvanceBookingDays",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1017)
                .OldAnnotation("Relational:ColumnOrder", 1014);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeDetailsSubmitted",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1023)
                .OldAnnotation("Relational:ColumnOrder", 1020);

            migrationBuilder.AlterColumn<bool>(
                name: "IsStripeChargesEnabled",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1024)
                .OldAnnotation("Relational:ColumnOrder", 1021);

            migrationBuilder.AlterColumn<bool>(
                name: "IsProfileComplete",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1019)
                .OldAnnotation("Relational:ColumnOrder", 1016);

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
                .OldAnnotation("Relational:ColumnOrder", 1018);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1020)
                .OldAnnotation("Relational:ColumnOrder", 1017);

            migrationBuilder.AlterColumn<string>(
                name: "IntroVideoUrl",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1014)
                .OldAnnotation("Relational:ColumnOrder", 1011);

            migrationBuilder.AlterColumn<int>(
                name: "CallPreference",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1015)
                .OldAnnotation("Relational:ColumnOrder", 1012);

            migrationBuilder.AddColumn<string>(
                name: "ServicesOffered",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1012);

            migrationBuilder.AddColumn<string>(
                name: "SpecialisedUniversities",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1011);

            migrationBuilder.AddColumn<string>(
                name: "SubjectAreas",
                schema: "consultant",
                table: "ConsultantProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1010);
        }
    }
}
