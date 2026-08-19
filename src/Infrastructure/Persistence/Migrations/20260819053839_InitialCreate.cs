using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace UrbanInfraSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    AreaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentAreaId = table.Column<int>(type: "int", nullable: true),
                    AreaCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AreaType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Boundary = table.Column<Geometry>(type: "geography", nullable: true),
                    CentroidLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CentroidLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.AreaId);
                    table.ForeignKey(
                        name: "FK_Areas_Areas_ParentAreaId",
                        column: x => x.ParentAreaId,
                        principalTable: "Areas",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    DepartmentCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "IssueTypes",
                columns: table => new
                {
                    IssueTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentIssueTypeId = table.Column<int>(type: "int", nullable: true),
                    TypeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTypes", x => x.IssueTypeId);
                    table.ForeignKey(
                        name: "FK_IssueTypes_IssueTypes_ParentIssueTypeId",
                        column: x => x.ParentIssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueId = table.Column<long>(type: "bigint", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "RoutingRules",
                columns: table => new
                {
                    RoutingRuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueTypeId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingRules", x => x.RoutingRuleId);
                    table.ForeignKey(
                        name: "FK_RoutingRules_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingRules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingRules_IssueTypes_IssueTypeId",
                        column: x => x.IssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueTypeId = table.Column<int>(type: "int", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "int", nullable: false),
                    FirstResponseMinutes = table.Column<int>(type: "int", nullable: false),
                    WarningBeforeMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                    table.CheckConstraint("CK_SlaPolicies_Minutes_Positive", "ResolutionMinutes > 0 AND FirstResponseMinutes > 0");
                    table.ForeignKey(
                        name: "FK_SlaPolicies_IssuePriorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "IssuePriorities",
                        principalColumn: "PriorityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlaPolicies_IssueTypes_IssueTypeId",
                        column: x => x.IssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentMembers",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsManager = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentMembers", x => new { x.DepartmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_DepartmentMembers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    IssueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    CustomTypeDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IssueTypeId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    DuplicateOfIssueId = table.Column<long>(type: "bigint", nullable: true),
                    UpvoteCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.IssueId);
                    table.ForeignKey(
                        name: "FK_Issues_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "AreaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_IssuePriorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "IssuePriorities",
                        principalColumn: "PriorityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_IssueStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "IssueStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_IssueTypes_IssueTypeId",
                        column: x => x.IssueTypeId,
                        principalTable: "IssueTypes",
                        principalColumn: "IssueTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "EscalationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverdueMinutes = table.Column<int>(type: "int", nullable: false),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: true),
                    TargetRoleName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "IssueAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    RoutingRuleId = table.Column<int>(type: "int", nullable: true),
                    AssignmentMethod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AssignmentNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignments", x => x.AssignmentId);
                    table.CheckConstraint("CK_IssueAssignments_Method", "AssignmentMethod IN ('AUTO', 'MANUAL', 'TRANSFER', 'ESCALATION')");
                    table.ForeignKey(
                        name: "FK_IssueAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueAssignments_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueAssignments_RoutingRules_RoutingRuleId",
                        column: x => x.RoutingRuleId,
                        principalTable: "RoutingRules",
                        principalColumn: "RoutingRuleId",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "IssueUpdates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FromStatusId = table.Column<int>(type: "int", nullable: true),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProgressPercent = table.Column<byte>(type: "tinyint", nullable: true),
                    IsSystemGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueUpdates_IssueStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "IssueStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueUpdates_IssueStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "IssueStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueUpdates_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueUpdates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueUpvotes",
                columns: table => new
                {
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    IssueId1 = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueUpvotes", x => new { x.IssueId, x.UserId });
                    table.ForeignKey(
                        name: "FK_IssueUpvotes_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueUpvotes_Issues_IssueId1",
                        column: x => x.IssueId1,
                        principalTable: "Issues",
                        principalColumn: "IssueId");
                });

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

            migrationBuilder.CreateTable(
                name: "EscalationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    EscalationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: true),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EventStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_Departments_TargetDepartmentId",
                        column: x => x.TargetDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_EscalationRules_EscalationRuleId",
                        column: x => x.EscalationRuleId,
                        principalTable: "EscalationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueAssignmentMembers",
                columns: table => new
                {
                    MemberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignmentMembers", x => x.MemberId);
                    table.CheckConstraint("CK_IssueAssignmentMembers_Status", "Status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_IssueAssignmentMembers_IssueAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "IssueAssignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueAttachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<long>(type: "bigint", nullable: false),
                    UpdateId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    WidthPx = table.Column<int>(type: "int", nullable: true),
                    HeightPx = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueAttachments_IssueUpdates_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "IssueUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueAttachments_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueAttachments_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_ParentAreaId",
                table: "Areas",
                column: "ParentAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMembers_UserId",
                table: "DepartmentMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_EscalationRuleId",
                table: "EscalationEvents",
                column: "EscalationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_IssueId",
                table: "EscalationEvents",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_TargetDepartmentId",
                table: "EscalationEvents",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_TargetDepartmentId",
                table: "EscalationRules",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_EscalationRules_UniqueCombination",
                table: "EscalationRules",
                columns: new[] { "SlaPolicyId", "EscalationLevel", "OverdueMinutes", "TargetDepartmentId", "TargetRoleName" },
                unique: true,
                filter: "[TargetDepartmentId] IS NOT NULL AND [TargetRoleName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_AssignedBy",
                table: "IssueAssignmentMembers",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_AssignmentId_UserId",
                table: "IssueAssignmentMembers",
                columns: new[] { "AssignmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignmentMembers_UserId",
                table: "IssueAssignmentMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_DepartmentId",
                table: "IssueAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_IssueId_IsCurrent",
                table: "IssueAssignments",
                columns: new[] { "IssueId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignments_RoutingRuleId",
                table: "IssueAssignments",
                column: "RoutingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAttachments_IssueId",
                table: "IssueAttachments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAttachments_UpdateId",
                table: "IssueAttachments",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAttachments_UploadedBy",
                table: "IssueAttachments",
                column: "UploadedBy");

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
                name: "IX_Issues_AreaId",
                table: "Issues",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueTypeId",
                table: "Issues",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Latitude",
                table: "Issues",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Longitude",
                table: "Issues",
                column: "Longitude");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PriorityId",
                table: "Issues",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PublicCode",
                table: "Issues",
                column: "PublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReportedAt",
                table: "Issues",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReporterId",
                table: "Issues",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReportId",
                table: "Issues",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_StatusId",
                table: "Issues",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueSlas_IssueId",
                table: "IssueSlas",
                column: "IssueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueSlas_SlaPolicyId",
                table: "IssueSlas",
                column: "SlaPolicyId");

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

            migrationBuilder.CreateIndex(
                name: "IX_IssueTypes_IsActive",
                table: "IssueTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTypes_ParentIssueTypeId",
                table: "IssueTypes",
                column: "ParentIssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTypes_TypeCode",
                table: "IssueTypes",
                column: "TypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpdates_CreatedAt",
                table: "IssueUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpdates_CreatedBy",
                table: "IssueUpdates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpdates_FromStatusId",
                table: "IssueUpdates",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpdates_IssueId",
                table: "IssueUpdates",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpdates_ToStatusId",
                table: "IssueUpdates",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpvotes_IssueId1",
                table: "IssueUpvotes",
                column: "IssueId1");

            migrationBuilder.CreateIndex(
                name: "IX_IssueUpvotes_UserId",
                table: "IssueUpvotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportIssueTypes_IssueTypeId",
                table: "ReportIssueTypes",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportIssueTypes_ReportId",
                table: "ReportIssueTypes",
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

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_AreaId_IssueTypeId",
                table: "RoutingRules",
                columns: new[] { "AreaId", "IssueTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_DepartmentId",
                table: "RoutingRules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_IssueTypeId",
                table: "RoutingRules",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_IssueTypeId_PriorityId",
                table: "SlaPolicies",
                columns: new[] { "IssueTypeId", "PriorityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_PriorityId",
                table: "SlaPolicies",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DepartmentMembers");

            migrationBuilder.DropTable(
                name: "EscalationEvents");

            migrationBuilder.DropTable(
                name: "IssueAssignmentMembers");

            migrationBuilder.DropTable(
                name: "IssueAttachments");

            migrationBuilder.DropTable(
                name: "IssueSlas");

            migrationBuilder.DropTable(
                name: "IssueUpvotes");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ReportIssueTypes");

            migrationBuilder.DropTable(
                name: "ReportUpvotes");

            migrationBuilder.DropTable(
                name: "ReRouteRequests");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "EscalationRules");

            migrationBuilder.DropTable(
                name: "IssueAssignments");

            migrationBuilder.DropTable(
                name: "IssueUpdates");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "SlaPolicies");

            migrationBuilder.DropTable(
                name: "RoutingRules");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "IssuePriorities");

            migrationBuilder.DropTable(
                name: "IssueStatuses");

            migrationBuilder.DropTable(
                name: "IssueTypes");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Areas");
        }
    }
}
