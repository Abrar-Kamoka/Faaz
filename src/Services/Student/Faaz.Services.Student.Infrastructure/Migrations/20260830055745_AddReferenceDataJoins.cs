using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Student.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceDataJoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpTypes",
                schema: "student",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TargetSubjects",
                schema: "student",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "TargetUniversities",
                schema: "student",
                table: "StudentProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePhotoUrl",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1012)
                .OldAnnotation("Relational:ColumnOrder", 1015);

            migrationBuilder.AlterColumn<int>(
                name: "ProfileCompleteness",
                schema: "student",
                table: "StudentProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1014)
                .OldAnnotation("Relational:ColumnOrder", 1017);

            migrationBuilder.AlterColumn<bool>(
                name: "IsOnboardingComplete",
                schema: "student",
                table: "StudentProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1015)
                .OldAnnotation("Relational:ColumnOrder", 1018);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1013)
                .OldAnnotation("Relational:ColumnOrder", 1016);

            migrationBuilder.CreateTable(
                name: "StudentProfileHelpServices",
                schema: "student",
                columns: table => new
                {
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfileHelpServices", x => new { x.StudentProfileId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_StudentProfileHelpServices_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalSchema: "student",
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfileTargetProgrammes",
                schema: "student",
                columns: table => new
                {
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfileTargetProgrammes", x => new { x.StudentProfileId, x.ProgrammeId });
                    table.ForeignKey(
                        name: "FK_StudentProfileTargetProgrammes_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalSchema: "student",
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfileTargetSubjects",
                schema: "student",
                columns: table => new
                {
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfileTargetSubjects", x => new { x.StudentProfileId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_StudentProfileTargetSubjects_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalSchema: "student",
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfileTargetUniversities",
                schema: "student",
                columns: table => new
                {
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfileTargetUniversities", x => new { x.StudentProfileId, x.UniversityId });
                    table.ForeignKey(
                        name: "FK_StudentProfileTargetUniversities_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalSchema: "student",
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentProfileHelpServices",
                schema: "student");

            migrationBuilder.DropTable(
                name: "StudentProfileTargetProgrammes",
                schema: "student");

            migrationBuilder.DropTable(
                name: "StudentProfileTargetSubjects",
                schema: "student");

            migrationBuilder.DropTable(
                name: "StudentProfileTargetUniversities",
                schema: "student");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePhotoUrl",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1015)
                .OldAnnotation("Relational:ColumnOrder", 1012);

            migrationBuilder.AlterColumn<int>(
                name: "ProfileCompleteness",
                schema: "student",
                table: "StudentProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1017)
                .OldAnnotation("Relational:ColumnOrder", 1014);

            migrationBuilder.AlterColumn<bool>(
                name: "IsOnboardingComplete",
                schema: "student",
                table: "StudentProfiles",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1018)
                .OldAnnotation("Relational:ColumnOrder", 1015);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1016)
                .OldAnnotation("Relational:ColumnOrder", 1013);

            migrationBuilder.AddColumn<int>(
                name: "HelpTypes",
                schema: "student",
                table: "StudentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 1014);

            migrationBuilder.AddColumn<string>(
                name: "TargetSubjects",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1012);

            migrationBuilder.AddColumn<string>(
                name: "TargetUniversities",
                schema: "student",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 1013);
        }
    }
}
