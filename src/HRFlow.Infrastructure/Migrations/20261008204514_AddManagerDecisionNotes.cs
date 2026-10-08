using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerDecisionNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "AuditEntries",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "AuditEntries");
        }
    }
}
