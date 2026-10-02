using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueLeaveRequestAuditTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM AuditEntries
                WHERE Id IN
                (
                    SELECT duplicateAudit.Id
                    FROM AuditEntries AS duplicateAudit
                    INNER JOIN AuditEntries AS canonicalAudit
                        ON duplicateAudit.LeaveRequestId = canonicalAudit.LeaveRequestId
                        AND duplicateAudit.Action = canonicalAudit.Action
                        AND duplicateAudit.ActorId = canonicalAudit.ActorId
                        AND duplicateAudit.NewStatus = canonicalAudit.NewStatus
                        AND (
                            duplicateAudit.OldStatus = canonicalAudit.OldStatus
                            OR (
                                duplicateAudit.OldStatus IS NULL
                                AND canonicalAudit.OldStatus IS NULL
                            )
                        )
                        AND (
                            duplicateAudit.Timestamp > canonicalAudit.Timestamp
                            OR (
                                duplicateAudit.Timestamp = canonicalAudit.Timestamp
                                AND duplicateAudit.Id > canonicalAudit.Id
                            )
                        )
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_LeaveRequestId",
                table: "AuditEntries");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_LeaveRequestId_Action_ActorId_OldStatus_NewStatus",
                table: "AuditEntries",
                columns: new[] { "LeaveRequestId", "Action", "ActorId", "OldStatus", "NewStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_LeaveRequestId_Action_ActorId_OldStatus_NewStatus",
                table: "AuditEntries");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_LeaveRequestId",
                table: "AuditEntries",
                column: "LeaveRequestId");
        }
    }
}
