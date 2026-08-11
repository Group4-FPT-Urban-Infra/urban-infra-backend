using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuePrioritiesAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssuePriorities",
                columns: table => new
                {
                    PriorityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriorityCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PriorityName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeverityRank = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuePriorities", x => x.PriorityId);
                });

            migrationBuilder.CreateTable(
                name: "IssueStatuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    StatusName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    IsPublicVisible = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueStatuses", x => x.StatusId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorities_PriorityCode",
                table: "IssuePriorities",
                column: "PriorityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorities_SeverityRank",
                table: "IssuePriorities",
                column: "SeverityRank",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatuses_DisplayOrder",
                table: "IssueStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatuses_StatusCode",
                table: "IssueStatuses",
                column: "StatusCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuePriorities");

            migrationBuilder.DropTable(
                name: "IssueStatuses");
        }
    }
}
