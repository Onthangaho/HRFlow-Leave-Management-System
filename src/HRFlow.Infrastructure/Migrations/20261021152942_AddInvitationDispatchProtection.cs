using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationDispatchProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationDispatchStartedAtUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationRecipientNormalizedEmail",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationDispatchStartedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivationRecipientNormalizedEmail",
                table: "AspNetUsers");
        }
    }
}
