using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionMode",
                table: "LeaveTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: "NotRequested");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceClass",
                table: "LeaveTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: "Ordinary");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceMode",
                table: "LeaveTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.AddColumn<string>(
                name: "RequirementInstructions",
                table: "LeaveTypes",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LeaveRequests",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionRequirements",
                table: "LeaveRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveTypes_Requirements",
                table: "LeaveTypes",
                sql: "DescriptionMode IN ('NotRequested','Optional','Required') AND EvidenceMode IN ('NotRequested','Optional','Required') AND EvidenceClass IN ('Medical','Ordinary') AND NOT (EvidenceClass = 'Medical' AND EvidenceMode = 'Required') AND (RequirementInstructions IS NULL OR length(RequirementInstructions) <= 1000)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveTypes_Requirements",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DescriptionMode",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "EvidenceClass",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "EvidenceMode",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequirementInstructions",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "SubmissionRequirements",
                table: "LeaveRequests");
        }
    }
}
