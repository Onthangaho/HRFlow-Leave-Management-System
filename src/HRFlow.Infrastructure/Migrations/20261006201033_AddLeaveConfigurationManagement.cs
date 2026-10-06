using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveConfigurationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot perform invariant Unicode casing. Stop for explicit review instead of
            // generating incorrect legacy keys or silently renaming/deleting colliding records.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE __LeaveConfigurationPreflight (Value INTEGER);
                CREATE TEMP TRIGGER __LeaveConfigurationPreflightCheck
                BEFORE INSERT ON __LeaveConfigurationPreflight
                BEGIN
                    SELECT RAISE(ABORT, 'Legacy leave type names require review: blank, over 100 characters, control characters, or non-ASCII. See ADR 0004.')
                    WHERE EXISTS (SELECT 1 FROM LeaveTypes WHERE Name IS NULL OR length(trim(Name)) NOT BETWEEN 1 AND 100 OR Name GLOB '*[^ -~]*' OR instr(Name, char(0)) > 0);
                    SELECT RAISE(ABORT, 'Legacy leave type names collide after trimming/case normalization. See ADR 0004.')
                    WHERE EXISTS (SELECT upper(trim(Name)) FROM LeaveTypes GROUP BY upper(trim(Name)) HAVING count(*) > 1);
                    SELECT RAISE(ABORT, 'Legacy negative leave entitlement requires review. See ADR 0004.')
                    WHERE EXISTS (SELECT 1 FROM LeavePolicies WHERE DefaultBalance < 0);
                    SELECT RAISE(ABORT, 'Legacy orphaned leave references require review. See ADR 0004.')
                    WHERE EXISTS (SELECT 1 FROM LeaveTypes t LEFT JOIN LeavePolicies p ON p.Id = t.LeavePolicyId WHERE p.Id IS NULL)
                       OR EXISTS (SELECT 1 FROM LeaveRequests r LEFT JOIN LeaveTypes t ON t.Id = r.LeaveTypeId WHERE t.Id IS NULL);
                END;
                INSERT INTO __LeaveConfigurationPreflight VALUES (1);
                DROP TRIGGER __LeaveConfigurationPreflightCheck;
                DROP TABLE __LeaveConfigurationPreflight;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_LeavePolicyId",
                table: "LeaveTypes");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "LeaveTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "LeaveTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "LeavePolicies",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "LeavePolicies",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // No previous policy labels existed. Add stable ID-based labels without changing any rule,
            // type name, relationship, request, or audit. Existing IDs seed nonempty edit versions.
            migrationBuilder.Sql("""
                UPDATE LeaveTypes SET NormalizedName = upper(trim(Name)), Version = Id;
                UPDATE LeavePolicies SET Name = 'Policy ' || Id, Version = Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_NormalizedName",
                table: "LeaveTypes",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveTypes_Name",
                table: "LeaveTypes",
                sql: "length(trim(Name)) BETWEEN 1 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeavePolicies_Balance",
                table: "LeavePolicies",
                sql: "DefaultBalance >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeavePolicies_Name",
                table: "LeavePolicies",
                sql: "length(trim(Name)) BETWEEN 1 AND 100");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_LeavePolicyId",
                table: "LeaveTypes",
                column: "LeavePolicyId",
                principalTable: "LeavePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_LeavePolicyId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_NormalizedName",
                table: "LeaveTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveTypes_Name",
                table: "LeaveTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LeavePolicies_Balance",
                table: "LeavePolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LeavePolicies_Name",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LeavePolicies");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_LeavePolicyId",
                table: "LeaveTypes",
                column: "LeavePolicyId",
                principalTable: "LeavePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
