using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEscalationTypeConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules");

            migrationBuilder.AlterColumn<string>(
                name: "EscalationType",
                table: "EscalationRules",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules",
                columns: new[] { "SlaPolicyId", "EscalationType", "EscalationLevel", "OverdueMinutes", "TargetDepartmentId", "TargetRoleName" },
                unique: true,
                filter: "[TargetDepartmentId] IS NOT NULL AND [TargetRoleName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules");

            migrationBuilder.AlterColumn<int>(
                name: "EscalationType",
                table: "EscalationRules",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules",
                columns: new[] { "SlaPolicyId", "EscalationLevel", "OverdueMinutes", "TargetDepartmentId", "TargetRoleName" },
                unique: true,
                filter: "[TargetDepartmentId] IS NOT NULL AND [TargetRoleName] IS NOT NULL");
        }
    }
}
