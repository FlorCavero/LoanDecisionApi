using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanDecisionApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "loan_applications",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "DelinquencyStatus",
                table: "loan_applications",
                type: "text",
                nullable: false,
                defaultValue: "Current");

            migrationBuilder.AddColumn<bool>(
                name: "IsCreditFreezeFlagged",
                table: "loan_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFraudRiskFlagged",
                table: "loan_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIdentityVerified",
                table: "loan_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDebtPayments",
                table: "loan_applications",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "rule_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParentRuleGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Grouping = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rule_groups_rule_groups_ParentRuleGroupId",
                        column: x => x.ParentRuleGroupId,
                        principalTable: "rule_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataPoint = table.Column<string>(type: "text", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rules_rule_groups_RuleGroupId",
                        column: x => x.RuleGroupId,
                        principalTable: "rule_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rule_groups_ParentRuleGroupId",
                table: "rule_groups",
                column: "ParentRuleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_rules_RuleGroupId",
                table: "rules",
                column: "RuleGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropTable(
                name: "rule_groups");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "loan_applications");

            migrationBuilder.DropColumn(
                name: "DelinquencyStatus",
                table: "loan_applications");

            migrationBuilder.DropColumn(
                name: "IsCreditFreezeFlagged",
                table: "loan_applications");

            migrationBuilder.DropColumn(
                name: "IsFraudRiskFlagged",
                table: "loan_applications");

            migrationBuilder.DropColumn(
                name: "IsIdentityVerified",
                table: "loan_applications");

            migrationBuilder.DropColumn(
                name: "MonthlyDebtPayments",
                table: "loan_applications");
        }
    }
}
