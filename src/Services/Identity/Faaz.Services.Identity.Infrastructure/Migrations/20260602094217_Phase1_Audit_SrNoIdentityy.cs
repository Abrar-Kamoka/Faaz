using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Audit_SrNoIdentityy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RefreshTokens.SrNo and PasswordResetTokens.SrNo are already IDENTITY(1,1)
            // from the preceding Phase1_Audit_SrNoIdentity migration.
            // This migration exists only to update the EF model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
