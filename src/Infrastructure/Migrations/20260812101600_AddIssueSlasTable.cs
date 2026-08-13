using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UrbanInfraSystem.Infrastructure.Persistence;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260812101600_AddIssueSlasTable")]
    public partial class AddIssueSlasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueSlas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstResponseMinutes = table.Column<int>(type: "int", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "int", nullable: false),
                    FirstResponseDueAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ResolutionDueAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    FirstRespondedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsFirstResponseBreached = table.Column<bool>(type: "bit", nullable: false),
                    IsResolutionBreached = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueSlas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueSlas_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueSlas_SlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "SlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueSlas_IssueId",
                table: "IssueSlas",
                column: "IssueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueSlas_SlaPolicyId",
                table: "IssueSlas",
                column: "SlaPolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueSlas");
        }
    }
}
