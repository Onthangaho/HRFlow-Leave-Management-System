using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnAccountSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SuppressedAtUtc",
                table: "LeaveNotificationEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountSettings",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreferredDisplayName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ProfileVersion = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreferencesVersion = table.Column<Guid>(type: "TEXT", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SubmissionNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    DecisionNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    CancellationNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReassignmentNotifications = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSettings", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_AccountSettings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountSettings");

            migrationBuilder.DropColumn(
                name: "SuppressedAtUtc",
                table: "LeaveNotificationEvents");
        }
    }
}
