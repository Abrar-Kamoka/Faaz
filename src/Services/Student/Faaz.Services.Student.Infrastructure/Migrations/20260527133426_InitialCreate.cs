using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Student.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    CountryOfCitizenship = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountryOfResidence = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ethnicity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstLanguage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdditionalLanguages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudyTrack = table.Column<int>(type: "int", nullable: true),
                    TargetStudyLevel = table.Column<int>(type: "int", nullable: true),
                    TargetSubjects = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUniversities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HelpTypes = table.Column<int>(type: "int", nullable: false),
                    ProfilePhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfileCompleteness = table.Column<int>(type: "int", nullable: false),
                    IsOnboardingComplete = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentPostgraduateData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: true),
                    UndergraduateUniversity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UndergraduateDegree = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UndergraduateGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostgraduateStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResearchInterests = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPostgraduateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPostgraduateData_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentSixthFormData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: true),
                    Subjects = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExamBoard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PredictedGrades = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    School = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TargetEntryYear = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSixthFormData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSixthFormData_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentUndergraduateData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: true),
                    CurrentUniversity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsGapYear = table.Column<bool>(type: "bit", nullable: false),
                    DegreeSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YearOfStudy = table.Column<int>(type: "int", nullable: true),
                    CurrentGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentUndergraduateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentUndergraduateData_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentPostgraduateData_StudentProfileId",
                table: "StudentPostgraduateData",
                column: "StudentProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_UserId",
                table: "StudentProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSixthFormData_StudentProfileId",
                table: "StudentSixthFormData",
                column: "StudentProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentUndergraduateData_StudentProfileId",
                table: "StudentUndergraduateData",
                column: "StudentProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentPostgraduateData");

            migrationBuilder.DropTable(
                name: "StudentSixthFormData");

            migrationBuilder.DropTable(
                name: "StudentUndergraduateData");

            migrationBuilder.DropTable(
                name: "StudentProfiles");
        }
    }
}
