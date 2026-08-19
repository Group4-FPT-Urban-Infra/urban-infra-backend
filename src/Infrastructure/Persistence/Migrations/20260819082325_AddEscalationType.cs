using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscalationType",
                table: "EscalationRules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationType",
                table: "EscalationRules");
        }
    }
}
