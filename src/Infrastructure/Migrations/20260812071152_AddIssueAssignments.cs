using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    RoutingRuleId = table.Column<int>(type: "int", nullable: true),
                    AssignmentMethod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AssignmentNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignments", x => x.AssignmentId);
                    table.CheckConstraint("CK_IssueAssignments_Method", "AssignmentMethod IN ('AUTO', 'MANUAL', 'TRANSFER', 'ESCALATION')");
                    table.ForeignKey(
                        name: "FK_IssueAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueAssignments_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueAssignments_RoutingRules_RoutingRuleId",
                        column: x => x.RoutingRuleId,
                        principalTable: "RoutingRules",
                        principalColumn: "RoutingRuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_DepartmentId",
                table: "IssueAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_IssueId_IsCurrent",
                table: "IssueAssignments",
                columns: new[] { "IssueId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_RoutingRuleId",
                table: "IssueAssignments",
                column: "RoutingRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueAssignments");
        }
    }
}
