using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Consultant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHighestQualificationOther : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HighestQualificationOther",
                schema: "consultant",
                table: "ConsultantApplications",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1026);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighestQualificationOther",
                schema: "consultant",
                table: "ConsultantApplications");
        }
    }
}
