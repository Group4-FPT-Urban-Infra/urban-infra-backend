using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportIssueTypeRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ReporterId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportUpvotes_Users_UserId",
                table: "ReportUpvotes");

            migrationBuilder.DropColumn(
                name: "UpvoteCount",
                table: "Reports");

            migrationBuilder.AddColumn<long>(
                name: "IssueId1",
                table: "IssueUpvotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportIssueTypes",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    IssueTypeId = table.Column<int>(type: "int", nullable: false),
                    IssueTypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IssueTypeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportIssueTypes", x => new { x.ReportId, x.IssueTypeId });
                    table.ForeignKey(
                        name: "FK_ReportIssueTypes_IssueTypes_IssueTypeId",
                        column: x => x.IssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportIssueTypes_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpvotes_IssueId1",
                table: "IssueUpvotes",
                column: "IssueId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReportIssueTypes_IssueTypeId",
                table: "ReportIssueTypes",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportIssueTypes_ReportId",
                table: "ReportIssueTypes",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueUpvotes_Issues_IssueId1",
                table: "IssueUpvotes",
                column: "IssueId1",
                principalTable: "Issues",
                principalColumn: "IssueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueUpvotes_Issues_IssueId1",
                table: "IssueUpvotes");

            migrationBuilder.DropTable(
                name: "ReportIssueTypes");

            migrationBuilder.DropIndex(
                name: "IX_IssueUpvotes_IssueId1",
                table: "IssueUpvotes");

            migrationBuilder.DropColumn(
                name: "IssueId1",
                table: "IssueUpvotes");

            migrationBuilder.AddColumn<int>(
                name: "UpvoteCount",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportUpvotes_Users_UserId",
                table: "ReportUpvotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
