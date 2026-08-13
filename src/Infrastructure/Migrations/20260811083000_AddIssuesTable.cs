using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    public partial class AddIssuesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    IssueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssueTypeId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    PriorityId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpvoteCount = table.Column<int>(type: "int", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.IssueId);
                    table.ForeignKey(
                        name: "FK_Issues_IssuePriorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "IssuePriorities",
                        principalColumn: "PriorityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_IssueStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "IssueStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_IssueTypes_IssueTypeId",
                        column: x => x.IssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueTypeId",
                table: "Issues",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PriorityId",
                table: "Issues",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_StatusId",
                table: "Issues",
                column: "StatusId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Issues");
        }
    }
}
