using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CredentialVersion",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Independent random generations avoid restoring an old proof during a later rollback/re-upgrade.
            migrationBuilder.Sql("UPDATE AspNetUsers SET CredentialVersion = hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6));");

            // Require fresh login after rollout; a pre-upgrade refresh must not mint upgraded bearer proof.
            migrationBuilder.Sql("UPDATE RefreshTokens SET RevokedAtUtc = CURRENT_TIMESTAMP WHERE RevokedAtUtc IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredentialVersion",
                table: "AspNetUsers");
        }
    }
}
