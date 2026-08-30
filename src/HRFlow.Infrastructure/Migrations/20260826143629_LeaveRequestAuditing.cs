using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeaveRequestAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedAt",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "ChangedBy",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditEntries");

            migrationBuilder.RenameColumn(
                name: "PropertyName",
                table: "AuditEntries",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "OldValue",
                table: "AuditEntries",
                newName: "LeaveRequestId");

            migrationBuilder.RenameColumn(
                name: "NewValue",
                table: "AuditEntries",
                newName: "ActorId");

            migrationBuilder.RenameColumn(
                name: "EntityName",
                table: "AuditEntries",
                newName: "Action");

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedById",
                table: "LeaveRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedOn",
                table: "LeaveRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewStatus",
                table: "AuditEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OldStatus",
                table: "AuditEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ProcessedById",
                table: "LeaveRequests",
                column: "ProcessedById");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorId",
                table: "AuditEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_LeaveRequestId",
                table: "AuditEntries",
                column: "LeaveRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_Employees_ActorId",
                table: "AuditEntries",
                column: "ActorId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_LeaveRequests_LeaveRequestId",
                table: "AuditEntries",
                column: "LeaveRequestId",
                principalTable: "LeaveRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Employees_ProcessedById",
                table: "LeaveRequests",
                column: "ProcessedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_Employees_ActorId",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_LeaveRequests_LeaveRequestId",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Employees_ProcessedById",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_ProcessedById",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_ActorId",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_LeaveRequestId",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "ProcessedById",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedOn",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "NewStatus",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "OldStatus",
                table: "AuditEntries");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AuditEntries",
                newName: "PropertyName");

            migrationBuilder.RenameColumn(
                name: "LeaveRequestId",
                table: "AuditEntries",
                newName: "OldValue");

            migrationBuilder.RenameColumn(
                name: "ActorId",
                table: "AuditEntries",
                newName: "NewValue");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AuditEntries",
                newName: "EntityName");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedAt",
                table: "AuditEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedBy",
                table: "AuditEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "AuditEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
