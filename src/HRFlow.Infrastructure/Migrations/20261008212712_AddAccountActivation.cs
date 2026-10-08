using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAtUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationExpiresAtUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationTokenHash",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationDeliveryState",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresActivation",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ActivationTokenHash",
                table: "AspNetUsers",
                column: "ActivationTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ActivationTokenHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivationExpiresAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivationTokenHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvitationDeliveryState",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RequiresActivation",
                table: "AspNetUsers");
        }
    }
}
