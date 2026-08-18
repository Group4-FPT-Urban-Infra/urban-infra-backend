using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireAttachmentIssueUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueAttachments_IssueUpdates_UpdateId",
                table: "IssueAttachments");

            // Dữ liệu cũ có thể gắn ảnh trực tiếp vào Issue. Tạo một update hệ thống
            // cho các Issue chưa có update, sau đó nối mọi attachment còn null vào
            // update đầu tiên của Issue trước khi siết cột thành NOT NULL.
            migrationBuilder.Sql(
                """
                INSERT INTO [IssueUpdates]
                    ([IssueId], [CreatedBy], [FromStatusId], [ToStatusId], [Note],
                     [ProgressPercent], [IsSystemGenerated], [CreatedAt])
                SELECT DISTINCT
                    i.[IssueId], i.[ReporterId], NULL, i.[StatusId],
                    N'Khởi tạo update để liên kết attachment hiện có.',
                    NULL, CAST(1 AS bit), CAST(SYSUTCDATETIME() AS datetime2(0))
                FROM [Issues] i
                INNER JOIN [IssueAttachments] a ON a.[IssueId] = i.[IssueId]
                WHERE a.[UpdateId] IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [IssueUpdates] u WHERE u.[IssueId] = i.[IssueId]
                  );

                UPDATE a
                SET a.[UpdateId] = (
                    SELECT MIN(u.[Id])
                    FROM [IssueUpdates] u
                    WHERE u.[IssueId] = a.[IssueId]
                )
                FROM [IssueAttachments] a
                WHERE a.[UpdateId] IS NULL;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "UpdateId",
                table: "IssueAttachments",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IssueAttachments_IssueUpdates_UpdateId",
                table: "IssueAttachments",
                column: "UpdateId",
                principalTable: "IssueUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueAttachments_IssueUpdates_UpdateId",
                table: "IssueAttachments");

            migrationBuilder.AlterColumn<long>(
                name: "UpdateId",
                table: "IssueAttachments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueAttachments_IssueUpdates_UpdateId",
                table: "IssueAttachments",
                column: "UpdateId",
                principalTable: "IssueUpdates",
                principalColumn: "Id");
        }
    }
}
