using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandApplicationAndAddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraField1",
                table: "ConsultantApplications");

            migrationBuilder.RenameColumn(
                name: "ExtraField2",
                table: "ConsultantApplications",
                newName: "LinkedInProfileUrl");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConsultationMode",
                table: "ConsultantApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfResidence",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "ConsultantApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HighestQualification",
                table: "ConsultantApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalStatement",
                table: "ConsultantApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ConsultantApplications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryLanguage",
                table: "ConsultantApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsultantApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantApplicationDocuments_ConsultantApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "ConsultantApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantApplicationDocuments_ApplicationId",
                table: "ConsultantApplicationDocuments",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultantApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ConsultationMode",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "CountryOfResidence",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "HighestQualification",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "PersonalStatement",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ConsultantApplications");

            migrationBuilder.DropColumn(
                name: "PrimaryLanguage",
                table: "ConsultantApplications");

            migrationBuilder.RenameColumn(
                name: "LinkedInProfileUrl",
                table: "ConsultantApplications",
                newName: "ExtraField2");

            migrationBuilder.AddColumn<string>(
                name: "ExtraField1",
                table: "ConsultantApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
