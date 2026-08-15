using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomTypeDescription",
                table: "Issues",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReportId",
                table: "Issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    PublicCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AddressText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    UpvoteCount = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportUpvotes",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportUpvotes", x => new { x.ReportId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ReportUpvotes_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportUpvotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Mỗi Issue legacy tương ứng một Report. Giữ nguyên ID giúp backfill an toàn
            // và không làm thay đổi contract công khai trong giai đoạn chuyển đổi.
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT [Reports] ON;
                INSERT INTO [Reports]
                    ([ReportId], [ReporterId], [AreaId], [PublicCode], [Title], [Description],
                     [AddressText], [Latitude], [Longitude], [UpvoteCount], [IsPublic], [IsArchived],
                     [ReportedAt], [CreatedAt], [UpdatedAt])
                SELECT [IssueId], [ReporterId], [AreaId], [PublicCode], [Title], [Description],
                       [AddressText], [Latitude], [Longitude], [UpvoteCount], [IsPublic], [IsArchived],
                       [ReportedAt], [ReportedAt], NULL
                FROM [Issues];
                SET IDENTITY_INSERT [Reports] OFF;

                UPDATE [Issues] SET [ReportId] = [IssueId];

                INSERT INTO [ReportUpvotes] ([ReportId], [UserId], [CreatedAt])
                SELECT [IssueId], [UserId], [CreatedAt] FROM [IssueUpvotes];
                """);

            migrationBuilder.AlterColumn<long>(
                name: "ReportId",
                table: "Issues",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReportId",
                table: "Issues",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AreaId",
                table: "Reports",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PublicCode",
                table: "Reports",
                column: "PublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedAt",
                table: "Reports",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUpvotes_UserId",
                table: "ReportUpvotes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Reports_ReportId",
                table: "Issues",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "ReportId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Reports_ReportId",
                table: "Issues");

            migrationBuilder.DropTable(
                name: "ReportUpvotes");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ReportId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "CustomTypeDescription",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "Issues");
        }
    }
}
