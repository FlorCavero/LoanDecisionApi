using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanDecisionApi.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDelinquencyStatusUnspecified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No LoanApplication has ever had DelinquencyStatus set by real user input yet
            // (the field isn't wired to any request DTO) - every "Current" value in the
            // database is an artifact of the AddRuleEngine migration's backfill default,
            // not a real assessed status. Safe to reset all of them to Unspecified.
            migrationBuilder.Sql(
                "UPDATE loan_applications SET \"DelinquencyStatus\" = 'Unspecified' WHERE \"DelinquencyStatus\" = 'Current';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE loan_applications SET \"DelinquencyStatus\" = 'Current' WHERE \"DelinquencyStatus\" = 'Unspecified';");
        }
    }
}
