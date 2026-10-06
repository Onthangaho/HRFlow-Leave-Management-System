using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeEditVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing identifiers are unique, non-empty initial edit tokens; preserve all profile/Identity history.
            migrationBuilder.Sql("UPDATE Employees SET Version = Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Employees");
        }
    }
}
