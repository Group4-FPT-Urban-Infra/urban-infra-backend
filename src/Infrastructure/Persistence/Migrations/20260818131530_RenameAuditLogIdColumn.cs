using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuditLogIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.AuditLogs', 'Id') IS NOT NULL
                   AND COL_LENGTH('dbo.AuditLogs', 'AuditLogId') IS NULL
                    EXEC sp_rename N'dbo.AuditLogs.Id', N'AuditLogId', N'COLUMN';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.AuditLogs', 'AuditLogId') IS NOT NULL
                   AND COL_LENGTH('dbo.AuditLogs', 'Id') IS NULL
                    EXEC sp_rename N'dbo.AuditLogs.AuditLogId', N'Id', N'COLUMN';
                """);
        }
    }
}
