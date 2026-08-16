using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "consultant");

            migrationBuilder.CreateTable(
                name: "ConsultantApplications",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryOfResidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsUkBased = table.Column<bool>(type: "bit", nullable: false),
                    CurrentRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpertiseArea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    HighestQualification = table.Column<int>(type: "int", nullable: true),
                    PrimaryLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedInProfileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonalStatement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsultationMode = table.Column<int>(type: "int", nullable: true),
                    ReferralSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationStatus = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SetupInviteToken = table.Column<string>(type: "nvarchar(88)", maxLength: 88, nullable: true),
                    SetupInviteTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SetupInviteSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantApplicationDocuments",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantApplicationDocuments_ConsultantApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantProfiles",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullLegalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessionalPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    StudyLevelsOffered = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectAreas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecialisedUniversities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServicesOffered = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WrittenBio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntroVideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallPreference = table.Column<int>(type: "int", nullable: false),
                    MinBookingNoticeHours = table.Column<int>(type: "int", nullable: false),
                    MaxAdvanceBookingDays = table.Column<int>(type: "int", nullable: false),
                    IsProfileComplete = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StripeAccountId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsStripeDetailsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    IsStripeChargesEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantProfiles_ConsultantApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantAvailabilitySlots",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsBlockedDate = table.Column<bool>(type: "bit", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    StartTimeUtc = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTimeUtc = table.Column<TimeOnly>(type: "time", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantAvailabilitySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantAvailabilitySlots_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantCredentials",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantCredentials_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantSessionTypes",
                schema: "consultant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    PriceGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantSessionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantSessionTypes_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalSchema: "consultant",
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplicationDocuments_ApplicationId",
                schema: "consultant",
                table: "ConsultantApplicationDocuments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplications_ApplicationStatus",
                schema: "consultant",
                table: "ConsultantApplications",
                column: "ApplicationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplications_Email",
                schema: "consultant",
                table: "ConsultantApplications",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplications_SetupInviteToken",
                schema: "consultant",
                table: "ConsultantApplications",
                column: "SetupInviteToken",
                filter: "[SetupInviteToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplications_UserId",
                schema: "consultant",
                table: "ConsultantApplications",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantAvailabilitySlots_ConsultantProfileId",
                schema: "consultant",
                table: "ConsultantAvailabilitySlots",
                column: "ConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantCredentials_ConsultantProfileId",
                schema: "consultant",
                table: "ConsultantCredentials",
                column: "ConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_ApplicationId",
                schema: "consultant",
                table: "ConsultantProfiles",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_IsActive_IsProfileComplete",
                schema: "consultant",
                table: "ConsultantProfiles",
                columns: new[] { "IsActive", "IsProfileComplete" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_UserId",
                schema: "consultant",
                table: "ConsultantProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantSessionTypes_ConsultantProfileId",
                schema: "consultant",
                table: "ConsultantSessionTypes",
                column: "ConsultantProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultantApplicationDocuments",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantAvailabilitySlots",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantCredentials",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantSessionTypes",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantProfiles",
                schema: "consultant");

            migrationBuilder.DropTable(
                name: "ConsultantApplications",
                schema: "consultant");
        }
    }
}
