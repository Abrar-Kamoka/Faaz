using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Student.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedConsultants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedConsultants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedConsultants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedConsultants_Student_Consultant",
                table: "SavedConsultants",
                columns: new[] { "StudentUserId", "ConsultantUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedConsultants");
        }
    }
}
