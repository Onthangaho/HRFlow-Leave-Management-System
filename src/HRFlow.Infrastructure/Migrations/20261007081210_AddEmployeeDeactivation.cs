using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAtUtc",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeactivatedById",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "Employees",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employees",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AuditEntries",
                type: "TEXT",
                maxLength: 522,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DeactivatedById",
                table: "Employees",
                column: "DeactivatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_DeactivatedById",
                table: "Employees",
                column: "DeactivatedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_DeactivatedById",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DeactivatedById",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeactivatedAtUtc",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeactivatedById",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AuditEntries");
        }
    }
}
