using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveNotificationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecipientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetryAfterUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveNotificationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveNotificationEvents_LeaveRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveNotifications_LeaveNotificationEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "LeaveNotificationEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveNotificationEvents_DeliveredAtUtc_CreatedAtUtc",
                table: "LeaveNotificationEvents",
                columns: new[] { "DeliveredAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveNotificationEvents_EventKey_RecipientId",
                table: "LeaveNotificationEvents",
                columns: new[] { "EventKey", "RecipientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveNotificationEvents_RequestId",
                table: "LeaveNotificationEvents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveNotifications_EventId_RecipientId",
                table: "LeaveNotifications",
                columns: new[] { "EventId", "RecipientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveNotifications_RecipientId_ReadAtUtc_CreatedAtUtc",
                table: "LeaveNotifications",
                columns: new[] { "RecipientId", "ReadAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveNotifications");

            migrationBuilder.DropTable(
                name: "LeaveNotificationEvents");
        }
    }
}
