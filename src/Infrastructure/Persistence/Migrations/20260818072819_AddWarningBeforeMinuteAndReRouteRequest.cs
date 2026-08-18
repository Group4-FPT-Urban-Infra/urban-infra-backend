using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarningBeforeMinuteAndReRouteRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarningBeforeMinutes",
                table: "SlaPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReRouteRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentDepartmentId = table.Column<int>(type: "int", nullable: false),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReRouteRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReRouteRequests_Departments_CurrentDepartmentId",
                        column: x => x.CurrentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReRouteRequests_Departments_TargetDepartmentId",
                        column: x => x.TargetDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReRouteRequests_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReRouteRequests_CurrentDepartmentId",
                table: "ReRouteRequests",
                column: "CurrentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReRouteRequests_IssueId_Status",
                table: "ReRouteRequests",
                columns: new[] { "IssueId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReRouteRequests_TargetDepartmentId",
                table: "ReRouteRequests",
                column: "TargetDepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReRouteRequests");

            migrationBuilder.DropColumn(
                name: "WarningBeforeMinutes",
                table: "SlaPolicies");
        }
    }
}
