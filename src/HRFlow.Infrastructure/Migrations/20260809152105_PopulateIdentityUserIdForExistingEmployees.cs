using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PopulateIdentityUserIdForExistingEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Update employees with exactly one matching AspNetUsers row (case-insensitive email match)
                UPDATE Employees
                SET IdentityUserId = (
                    SELECT Id
                    FROM AspNetUsers
                    WHERE UPPER(NormalizedEmail) = UPPER(Employees.Email)
                )
                WHERE IdentityUserId IS NULL
                  AND (
                    SELECT COUNT(*)
                    FROM AspNetUsers
                    WHERE UPPER(NormalizedEmail) = UPPER(Employees.Email)
                  ) = 1;

                -- Report employees that could not be matched (zero or multiple matches)
                SELECT
                    Id,
                    Email,
                    CASE
                        WHEN (SELECT COUNT(*) FROM AspNetUsers WHERE UPPER(NormalizedEmail) = UPPER(Employees.Email)) = 0
                        THEN 'No matching AspNetUsers account found'
                        ELSE 'Multiple matching AspNetUsers accounts found (ambiguous)'
                    END AS Reason
                FROM Employees
                WHERE IdentityUserId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}