using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanDecisionApi.Migrations
{
    /// <inheritdoc />
    public partial class RuleGroupAsJsonTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rule_groups_rule_groups_ParentRuleGroupId",
                table: "rule_groups");

            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropIndex(
                name: "IX_rule_groups_ParentRuleGroupId",
                table: "rule_groups");

            migrationBuilder.DropColumn(
                name: "ParentRuleGroupId",
                table: "rule_groups");

            migrationBuilder.AddColumn<string>(
                name: "ChildGroups",
                table: "rule_groups",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Rules",
                table: "rule_groups",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildGroups",
                table: "rule_groups");

            migrationBuilder.DropColumn(
                name: "Rules",
                table: "rule_groups");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentRuleGroupId",
                table: "rule_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: false),
                    DataPoint = table.Column<string>(type: "text", nullable: false),
                    RuleGroupId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.AddForeignKey(
                name: "FK_rule_groups_rule_groups_ParentRuleGroupId",
                table: "rule_groups",
                column: "ParentRuleGroupId",
                principalTable: "rule_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
