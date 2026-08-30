using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Administration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceDataCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1007)
                .OldAnnotation("Relational:ColumnOrder", 1002);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "admin",
                table: "Universities",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1009)
                .OldAnnotation("Relational:ColumnOrder", 1003);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1002)
                .OldAnnotation("Relational:ColumnOrder", 1001);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1004);

            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1010);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionType",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1005);

            migrationBuilder.AddColumn<bool>(
                name: "IsRussellGroup",
                schema: "admin",
                table: "Universities",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 1006);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                schema: "admin",
                table: "Universities",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1012);

            migrationBuilder.AddColumn<string>(
                name: "Nation",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1003);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1011);

            migrationBuilder.AddColumn<string>(
                name: "Ukprn",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1001);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1008);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "admin",
                table: "Subjects",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1003)
                .OldAnnotation("Relational:ColumnOrder", 1002);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "admin",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1002)
                .OldAnnotation("Relational:ColumnOrder", 1001);

            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                schema: "admin",
                table: "Subjects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1004);

            migrationBuilder.AddColumn<string>(
                name: "HecosCode",
                schema: "admin",
                table: "Subjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1001);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                schema: "admin",
                table: "Subjects",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1006);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                schema: "admin",
                table: "Subjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1005);

            migrationBuilder.CreateTable(
                name: "Programmes",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StudyLevel = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: true),
                    UcasCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EntryRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TuitionFeeDomesticGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    TuitionFeeInternationalGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Programmes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Programmes_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalSchema: "admin",
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceDataRequests",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    ProposedName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByAdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ReferenceDataRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeSubjects",
                schema: "admin",
                columns: table => new
                {
                    ProgrammeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeSubjects", x => new { x.ProgrammeId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_ProgrammeSubjects_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalSchema: "admin",
                        principalTable: "Programmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgrammeSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "admin",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Universities_Ukprn",
                schema: "admin",
                table: "Universities",
                column: "Ukprn",
                filter: "[IsDeleted] = 0 AND [Ukprn] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_HecosCode",
                schema: "admin",
                table: "Subjects",
                column: "HecosCode",
                filter: "[IsDeleted] = 0 AND [HecosCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_Title",
                schema: "admin",
                table: "Programmes",
                column: "Title",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_UniversityId_StudyLevel",
                schema: "admin",
                table: "Programmes",
                columns: new[] { "UniversityId", "StudyLevel" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeSubjects_SubjectId",
                schema: "admin",
                table: "ProgrammeSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceDataRequests_Status",
                schema: "admin",
                table: "ReferenceDataRequests",
                column: "Status",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                schema: "admin",
                table: "Services",
                column: "Name",
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgrammeSubjects",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "ReferenceDataRequests",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "Programmes",
                schema: "admin");

            migrationBuilder.DropIndex(
                name: "IX_Universities_Ukprn",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_HecosCode",
                schema: "admin",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "DataSource",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "InstitutionType",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "IsRussellGroup",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Nation",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "Ukprn",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                schema: "admin",
                table: "Universities");

            migrationBuilder.DropColumn(
                name: "DataSource",
                schema: "admin",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "HecosCode",
                schema: "admin",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                schema: "admin",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                schema: "admin",
                table: "Subjects");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1002)
                .OldAnnotation("Relational:ColumnOrder", 1007);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "admin",
                table: "Universities",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1003)
                .OldAnnotation("Relational:ColumnOrder", 1009);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                schema: "admin",
                table: "Universities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1001)
                .OldAnnotation("Relational:ColumnOrder", 1002);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "admin",
                table: "Subjects",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit")
                .Annotation("Relational:ColumnOrder", 1002)
                .OldAnnotation("Relational:ColumnOrder", 1003);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "admin",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 1001)
                .OldAnnotation("Relational:ColumnOrder", 1002);
        }
    }
}
