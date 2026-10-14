using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCsvOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceHash = table.Column<string>(type: "TEXT", nullable: false),
                    MappingJson = table.Column<string>(type: "TEXT", nullable: false),
                    RowsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Cancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SelectedRowsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ConfirmationKey = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MinutesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingBatches_ActorId_ConfirmationKey",
                table: "OnboardingBatches",
                columns: new[] { "ActorId", "ConfirmationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingBatches_ExpiresAtUtc",
                table: "OnboardingBatches",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingScheduleTemplates_Code",
                table: "OnboardingScheduleTemplates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingBatches");

            migrationBuilder.DropTable(
                name: "OnboardingScheduleTemplates");
        }
    }
}
