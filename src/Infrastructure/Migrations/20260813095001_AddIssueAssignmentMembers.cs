using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueAssignmentMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueAssignmentMembers",
                columns: table => new
                {
                    MemberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignmentMembers", x => x.MemberId);
                    table.CheckConstraint("CK_IssueAssignmentMembers_Status", "Status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_IssueAssignmentMembers_IssueAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "IssueAssignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_AssignedBy",
                table: "IssueAssignmentMembers",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_AssignmentId_UserId",
                table: "IssueAssignmentMembers",
                columns: new[] { "AssignmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_UserId",
                table: "IssueAssignmentMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueAssignmentMembers");
        }
    }
}
