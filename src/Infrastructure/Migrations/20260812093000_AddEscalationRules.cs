using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    public partial class AddEscalationRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscalationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverdueMinutes = table.Column<int>(type: "int", nullable: false),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: true),
                    TargetRoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NotificationTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NotificationTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationRules_Departments_TargetDepartmentId",
                        column: x => x.TargetDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationRules_SlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "SlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_SlaPolicyId",
                table: "EscalationRules",
                column: "SlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_TargetDepartmentId",
                table: "EscalationRules",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules",
                columns: new[] { "SlaPolicyId", "EscalationLevel", "OverdueMinutes", "TargetDepartmentId", "TargetRoleName" },
                unique: true,
                filter: "IsDeleted = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalationRules");
        }
    }
}
