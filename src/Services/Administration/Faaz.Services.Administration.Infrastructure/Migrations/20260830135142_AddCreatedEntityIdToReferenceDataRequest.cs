using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Administration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedEntityIdToReferenceDataRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedEntityId",
                schema: "admin",
                table: "ReferenceDataRequests",
                type: "uniqueidentifier",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 1009);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedEntityId",
                schema: "admin",
                table: "ReferenceDataRequests");
        }
    }
}
