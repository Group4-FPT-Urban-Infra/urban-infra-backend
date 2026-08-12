using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    public partial class AddIssueSlasAndEscEventUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueSlas",
                columns: table => new
                {
                    IssueSlaId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolutionDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueSlas", x => x.IssueSlaId);
                    table.ForeignKey(
                        name: "FK_IssueSlas_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueSlas_IssueId",
                table: "IssueSlas",
                column: "IssueId");

            // Ensure uniqueness to prevent duplicate escalation events in case of concurrent inserts
            migrationBuilder.CreateIndex(
                name: "UX_EscalationEvents_Issue_Rule",
                table: "EscalationEvents",
                columns: new[] { "IssueId", "EscalationRuleId" },
                unique: true,
                filter: "IsDeleted = 0 AND EscalationRuleId IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EscalationEvents_Issue_Rule",
                table: "EscalationEvents");

            migrationBuilder.DropTable(
                name: "IssueSlas");
        }
    }
}
