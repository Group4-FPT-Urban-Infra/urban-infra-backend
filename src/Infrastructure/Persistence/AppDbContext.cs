using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;

namespace UrbanInfraSystem.Infrastructure.Persistence;

/// <summary>
/// DbContext gốc, kế thừa IdentityDbContext để có sẵn các bảng Users/Roles/Claims của ASP.NET Core Identity.
/// Khai báo đầy đủ các DbSet nghiệp vụ cho hệ thống quản lý hạ tầng đô thị.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();
    public DbSet<IssuePriority> IssuePriorities => Set<IssuePriority>();
    public DbSet<IssueStatus> IssueStatuses => Set<IssueStatus>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentMember> DepartmentMembers => Set<DepartmentMember>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueAttachment> IssueAttachments => Set<IssueAttachment>();
    public DbSet<IssueUpdate> IssueUpdates => Set<IssueUpdate>();
    public DbSet<IssueAssignment> IssueAssignments => Set<IssueAssignment>();
    public DbSet<IssueAssignmentMember> IssueAssignmentMembers => Set<IssueAssignmentMember>();
    public DbSet<IssueSla> IssueSlas => Set<IssueSla>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<EscalationEvent> EscalationEvents => Set<EscalationEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportIssueType> ReportIssueTypes => Set<ReportIssueType>();
    public DbSet<ReportUpvote> ReportUpvotes => Set<ReportUpvote>();
    public DbSet<IssueUpvote> IssueUpvotes => Set<IssueUpvote>();
    public DbSet<ReRouteRequest> ReRouteRequests => Set<ReRouteRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Đổi tên các bảng ASP.NET Core Identity
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // 2. RefreshTokens
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(512);
            entity.HasIndex(rt => rt.Token).IsUnique();

            entity.HasOne<ApplicationUser>()
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Areas
        builder.Entity<Area>(entity =>
        {
            entity.ToTable("Areas");
            entity.HasKey(a => a.AreaId);

            entity.Property(a => a.AreaCode).IsRequired().HasMaxLength(30);
            entity.Property(a => a.AreaName).IsRequired().HasMaxLength(150);
            entity.Property(a => a.AreaType).IsRequired().HasMaxLength(30);
            entity.Property(a => a.Boundary).HasColumnType("geography");
            entity.Property(a => a.CentroidLatitude).HasPrecision(9, 6);
            entity.Property(a => a.CentroidLongitude).HasPrecision(9, 6);
            entity.HasOne(a => a.ParentArea)
                  .WithMany(a => a.SubAreas)
                  .HasForeignKey(a => a.ParentAreaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. IssueTypes
        builder.Entity<IssueType>(entity =>
        {
            entity.ToTable("IssueTypes");
            entity.HasKey(it => it.IssueTypeId);
            entity.Property(it => it.IssueTypeId).ValueGeneratedOnAdd();
            entity.Property(it => it.TypeCode).IsRequired().HasMaxLength(30);
            entity.Property(it => it.TypeName).IsRequired().HasMaxLength(150).IsUnicode(true);
            entity.Property(it => it.IconUrl).HasMaxLength(1000).IsUnicode(true);
            entity.Property(it => it.Description).HasMaxLength(2000).IsUnicode(true);

            entity.HasIndex(it => it.TypeCode).IsUnique().HasDatabaseName("IX_IssueTypes_TypeCode");

            entity.HasOne(it => it.ParentIssueType)
                  .WithMany(it => it.SubIssueTypes)
                  .HasForeignKey(it => it.ParentIssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(it => it.ParentIssueTypeId).HasDatabaseName("IX_IssueTypes_ParentIssueTypeId");
            entity.HasIndex(it => it.IsActive).HasDatabaseName("IX_IssueTypes_IsActive");
        });

        // 5. IssuePriorities
        builder.Entity<IssuePriority>(entity =>
        {
            entity.ToTable("IssuePriorities");
            entity.HasKey(x => x.PriorityId);
            entity.Property(x => x.PriorityId).ValueGeneratedOnAdd();
            entity.Property(x => x.PriorityCode).IsRequired().HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.PriorityName).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.PriorityCode).IsUnique();
            entity.HasIndex(x => x.SeverityRank).IsUnique();
        });

        // 6. IssueStatuses
        builder.Entity<IssueStatus>(entity =>
        {
            entity.ToTable("IssueStatuses");
            entity.HasKey(x => x.StatusId);
            entity.Property(x => x.StatusId).ValueGeneratedOnAdd();
            entity.Property(x => x.StatusCode).IsRequired().HasMaxLength(30).IsUnicode(false);
            entity.Property(x => x.StatusName).IsRequired().HasMaxLength(80);
            entity.HasIndex(x => x.StatusCode).IsUnique();
            entity.HasIndex(x => x.DisplayOrder).IsUnique();
        });

        // 7. Departments
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(x => x.DepartmentId);
            entity.Property(x => x.DepartmentId).ValueGeneratedOnAdd();
            entity.Property(x => x.DepartmentCode).IsRequired().HasMaxLength(30).IsUnicode(false);
            entity.Property(x => x.DepartmentName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.DepartmentCode).IsUnique();
            entity.HasOne(x => x.ParentDepartment)
                  .WithMany(x => x.ChildDepartments)
                  .HasForeignKey(x => x.ParentDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. DepartmentMembers
        builder.Entity<DepartmentMember>(entity =>
        {
            entity.ToTable("DepartmentMembers");
            entity.HasKey(x => new { x.DepartmentId, x.UserId });
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.JobTitle).HasMaxLength(120);
            entity.Property(x => x.JoinedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.LeftAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.Department)
                  .WithMany(x => x.Members)
                  .HasForeignKey(x => x.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. SlaPolicies
        builder.Entity<SlaPolicy>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_SlaPolicies_Minutes_Positive", "ResolutionMinutes > 0 AND FirstResponseMinutes > 0");
            });

            entity.HasKey(s => s.Id);

            entity.Property(s => s.ResolutionMinutes).IsRequired();
            entity.Property(s => s.FirstResponseMinutes).IsRequired();

            entity.HasOne(s => s.IssueType)
                  .WithMany(it => it.SlaPolicies)
                  .HasForeignKey(s => s.IssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasOne(s => s.IssuePriority)
                  .WithMany(p => p.SlaPolicies)
                  .HasForeignKey(s => s.PriorityId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasIndex(s => new { s.IssueTypeId, s.PriorityId }).IsUnique();
        });

        // 10. Reports - dữ liệu phản ánh do Citizen gửi.
        builder.Entity<Report>(entity =>
        {
            entity.ToTable("Reports");
            entity.HasKey(x => x.ReportId);
            entity.Property(x => x.ReportId).ValueGeneratedOnAdd();
            entity.Property(x => x.ReporterId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.PublicCode).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(4000);
            entity.Property(x => x.AddressText).HasMaxLength(500);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.ReportedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.PublicCode).IsUnique();
            entity.HasIndex(x => x.ReporterId);
            entity.HasIndex(x => x.AreaId);
            entity.HasIndex(x => x.ReportedAt);
            entity.HasOne(x => x.Area)
                  .WithMany()
                  .HasForeignKey(x => x.AreaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 10b. ReportIssueTypes (junction N-N)
        builder.Entity<ReportIssueType>(entity =>
        {
            entity.ToTable("ReportIssueTypes");
            entity.HasKey(x => new { x.ReportId, x.IssueTypeId });
            entity.Property(x => x.IssueTypeName).IsRequired().HasMaxLength(150).IsUnicode(true);
            entity.Property(x => x.IssueTypeCode).IsRequired().HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.ReportId);
            entity.HasOne(x => x.Report)
                  .WithMany(r => r.ReportIssueTypes)
                  .HasForeignKey(x => x.ReportId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.IssueType)
                  .WithMany()
                  .HasForeignKey(x => x.IssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. Issues
        builder.Entity<Issue>(entity =>
        {
            entity.ToTable("Issues");
            entity.HasKey(i => i.IssueId);
            entity.Property(i => i.IssueId).ValueGeneratedOnAdd();
            entity.Property(i => i.CustomTypeDescription).HasMaxLength(1000);

            entity.Property(i => i.PublicCode).IsRequired().HasMaxLength(30);
            entity.Property(i => i.ReporterId).IsRequired().HasMaxLength(450);
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Description).IsRequired();
            entity.Property(i => i.AddressText).HasMaxLength(500);
            entity.Property(i => i.Latitude).HasPrecision(9, 6);
            entity.Property(i => i.Longitude).HasPrecision(9, 6);
            entity.Property(i => i.ReportedAt).HasColumnType("datetime2(0)");
            entity.Property(i => i.ResolvedAt).HasColumnType("datetime2(0)");
            entity.Property(i => i.ClosedAt).HasColumnType("datetime2(0)");

            entity.HasIndex(i => i.PublicCode).IsUnique().HasDatabaseName("IX_Issues_PublicCode");
            entity.HasIndex(i => i.ReportId).HasDatabaseName("IX_Issues_ReportId");
            entity.HasIndex(i => i.ReporterId).HasDatabaseName("IX_Issues_ReporterId");
            entity.HasIndex(i => i.StatusId).HasDatabaseName("IX_Issues_StatusId");
            entity.HasIndex(i => i.ReportedAt).HasDatabaseName("IX_Issues_ReportedAt");
            entity.HasIndex(i => i.Latitude).HasDatabaseName("IX_Issues_Latitude");
            entity.HasIndex(i => i.Longitude).HasDatabaseName("IX_Issues_Longitude");

            entity.HasOne(i => i.IssueType)
                  .WithMany()
                  .HasForeignKey(i => i.IssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Area)
                  .WithMany()
                  .HasForeignKey(i => i.AreaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Report)
                  .WithMany(r => r.Issues)
                  .HasForeignKey(i => i.ReportId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Priority)
                  .WithMany()
                  .HasForeignKey(i => i.PriorityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Status)
                  .WithMany()
                  .HasForeignKey(i => i.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. IssueAttachments
        builder.Entity<IssueAttachment>(entity =>
        {
            entity.ToTable("IssueAttachments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedOnAdd();

            entity.Property(a => a.UploadedBy).IsRequired().HasMaxLength(450);
            entity.Property(a => a.UpdateId).IsRequired();
            entity.Property(a => a.Kind).IsRequired().HasMaxLength(20);
            entity.Property(a => a.FileUrl).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(a => a.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.CreatedAt).HasColumnType("datetime2(0)");

            entity.HasIndex(a => a.IssueId).HasDatabaseName("IX_IssueAttachments_IssueId");
            entity.HasIndex(a => a.UpdateId).HasDatabaseName("IX_IssueAttachments_UpdateId");

            entity.HasOne(a => a.Issue)
                  .WithMany(i => i.Attachments)
                  .HasForeignKey(a => a.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Update)
                  .WithMany(u => u.Attachments)
                  .HasForeignKey(a => a.UpdateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(a => a.UploadedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. IssueUpdates
        builder.Entity<IssueUpdate>(entity =>
        {
            entity.ToTable("IssueUpdates");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();

            entity.Property(u => u.CreatedBy).IsRequired().HasMaxLength(450);
            entity.Property(u => u.Note).HasMaxLength(4000);
            entity.Property(u => u.CreatedAt).HasColumnType("datetime2(0)");

            entity.HasIndex(u => u.IssueId).HasDatabaseName("IX_IssueUpdates_IssueId");
            entity.HasIndex(u => u.CreatedAt).HasDatabaseName("IX_IssueUpdates_CreatedAt");

            entity.HasOne(u => u.Issue)
                  .WithMany(i => i.Updates)
                  .HasForeignKey(u => u.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.FromStatus)
                  .WithMany()
                  .HasForeignKey(u => u.FromStatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.ToStatus)
                  .WithMany()
                  .HasForeignKey(u => u.ToStatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(u => u.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 14. RoutingRules
        builder.Entity<RoutingRule>(entity =>
        {
            entity.ToTable("RoutingRules");
            entity.HasKey(x => x.RoutingRuleId);
            entity.Property(x => x.RoutingRuleId).ValueGeneratedOnAdd();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => new { x.AreaId, x.IssueTypeId }).IsUnique();
            entity.HasIndex(x => x.DepartmentId);

            entity.HasOne(x => x.IssueType)
                  .WithMany()
                  .HasForeignKey(x => x.IssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Area)
                  .WithMany()
                  .HasForeignKey(x => x.AreaId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department)
                  .WithMany()
                  .HasForeignKey(x => x.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 15. IssueAssignments
        builder.Entity<IssueAssignment>(entity =>
        {
            entity.ToTable("IssueAssignments", table =>
                table.HasCheckConstraint("CK_IssueAssignments_Method", "AssignmentMethod IN ('AUTO', 'MANUAL', 'TRANSFER', 'ESCALATION')"));
            entity.HasKey(x => x.AssignmentId);
            entity.Property(x => x.AssignmentId).ValueGeneratedOnAdd();
            entity.Property(x => x.AssignmentMethod).IsRequired().HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.AssignmentNote).HasMaxLength(1000);
            entity.Property(x => x.AssignedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.AcceptedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.EndedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.DepartmentId);
            entity.HasIndex(x => x.RoutingRuleId);
            entity.HasIndex(x => new { x.IssueId, x.IsCurrent });

            entity.HasOne(x => x.Issue)
                  .WithMany(x => x.Assignments)
                  .HasForeignKey(x => x.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Department)
                  .WithMany()
                  .HasForeignKey(x => x.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RoutingRule)
                  .WithMany()
                  .HasForeignKey(x => x.RoutingRuleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 15b. IssueAssignmentMembers
        builder.Entity<IssueAssignmentMember>(entity =>
        {
            entity.ToTable("IssueAssignmentMembers", table =>
                table.HasCheckConstraint("CK_IssueAssignmentMembers_Status", "Status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'COMPLETED')"));
            entity.HasKey(x => x.MemberId);
            entity.Property(x => x.MemberId).ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.AssignedBy).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.AssignedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.AcceptedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.EndedAt).HasColumnType("datetime2(0)");

            entity.HasIndex(x => new { x.AssignmentId, x.UserId }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.AssignedBy);

            entity.HasOne(x => x.Assignment)
                  .WithMany(x => x.Members)
                  .HasForeignKey(x => x.AssignmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 16. IssueSlas
        builder.Entity<IssueSla>(entity =>
        {
            entity.ToTable("IssueSlas");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();

            entity.Property(s => s.FirstResponseDueAt).HasColumnType("datetime2(0)");
            entity.Property(s => s.ResolutionDueAt).HasColumnType("datetime2(0)");
            entity.Property(s => s.FirstRespondedAt).HasColumnType("datetime2(0)");
            entity.Property(s => s.ResolvedAt).HasColumnType("datetime2(0)");
            entity.Property(s => s.CreatedAt).HasColumnType("datetime2(0)");

            entity.HasIndex(s => s.IssueId).IsUnique().HasDatabaseName("IX_IssueSlas_IssueId");

            entity.HasOne(s => s.Issue)
                  .WithOne(i => i.Sla)
                  .HasForeignKey<IssueSla>(s => s.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.SlaPolicy)
                  .WithMany()
                  .HasForeignKey(s => s.SlaPolicyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 17. EscalationRules
        builder.Entity<EscalationRule>(entity =>
        {
            entity.ToTable("EscalationRules");
            entity.HasKey(x => x.Id);

            // Note: BaseEntity columns (CreatedAtUtc, UpdatedAtUtc, etc.) are not mapped
            // because the database table doesn't have those columns

            entity.Property(x => x.SlaPolicyId).IsRequired();
            entity.Property(x => x.OverdueMinutes).IsRequired();
            entity.Property(x => x.TargetRoleName).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.EscalationLevel).IsRequired();
            entity.Property(x => x.NotificationTitle).HasMaxLength(250);
            entity.Property(x => x.NotificationTemplate).HasMaxLength(4000);
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.SlaPolicy)
                  .WithMany()
                  .HasForeignKey(x => x.SlaPolicyId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();

            entity.HasOne(x => x.TargetDepartment)
                  .WithMany()
                  .HasForeignKey(x => x.TargetDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.SlaPolicyId, x.EscalationLevel, x.OverdueMinutes, x.TargetDepartmentId, x.TargetRoleName })
                  .IsUnique()
                  .HasDatabaseName("UX_EscalationRules_UniqueCombination");
        });

        // 18. EscalationEvents
        builder.Entity<EscalationEvent>(entity =>
        {
            entity.ToTable("EscalationEvents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.IssueId).IsRequired();
            entity.Property(x => x.TargetUserId).HasMaxLength(450);
            entity.Property(x => x.TriggeredAt).IsRequired();
            entity.Property(x => x.AcknowledgedBy).HasMaxLength(450);
            entity.Property(x => x.EventStatus).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.Note).HasMaxLength(1000);

            entity.HasOne(x => x.Issue)
                  .WithMany()
                  .HasForeignKey(x => x.IssueId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EscalationRule)
                  .WithMany()
                  .HasForeignKey(x => x.EscalationRuleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetDepartment)
                  .WithMany()
                  .HasForeignKey(x => x.TargetDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.IssueId).HasDatabaseName("IX_EscalationEvents_IssueId");
            entity.HasIndex(x => x.TargetDepartmentId).HasDatabaseName("IX_EscalationEvents_TargetDepartmentId");
        });

        // 19. Notifications
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.NotificationType).HasMaxLength(50);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.IsRead }).HasDatabaseName("IX_Notifications_UserId_IsRead");
        });

        // 20. AuditLogs
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("audit_log_id");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id").HasMaxLength(450);
            entity.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(50);
            entity.Property(x => x.EntityName).HasColumnName("entity_name").IsRequired().HasMaxLength(100);
            entity.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
            entity.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("nvarchar(max)");
            entity.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("nvarchar(max)");
            entity.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            entity.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("datetime2(0)");

            // Map relationship to ApplicationUser without navigation property in AuditLog
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(x => x.ActorUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OccurredAt).HasDatabaseName("IX_AuditLogs_OccurredAt");
        });

        // 21. ReRouteRequests
        builder.Entity<ReRouteRequest>(entity =>
        {
            entity.ToTable("ReRouteRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsUnicode(false);
            entity.Property(x => x.RequestedBy).IsRequired().HasMaxLength(450);
            entity.Property(x => x.RequestedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.ProcessedAt).HasColumnType("datetime2(0)");
            entity.Property(x => x.ProcessedBy).HasMaxLength(450);

            entity.HasIndex(x => new { x.IssueId, x.Status })
                  .HasDatabaseName("IX_ReRouteRequests_IssueId_Status");

            entity.HasOne(x => x.Issue)
                  .WithMany()
                  .HasForeignKey(x => x.IssueId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasOne(x => x.CurrentDepartment)
                  .WithMany()
                  .HasForeignKey(x => x.CurrentDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();

            entity.HasOne(x => x.TargetDepartment)
                  .WithMany()
                  .HasForeignKey(x => x.TargetDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();
        });

        // 22. IssueUpvotes
        builder.Entity<IssueUpvote>(entity =>
        {
            entity.ToTable("IssueUpvotes");
            entity.HasKey(x => new { x.IssueId, x.UserId });
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.Issue)
                  .WithMany()
                  .HasForeignKey(x => x.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 23. ReportUpvotes (legacy - kept for migration compatibility)
        builder.Entity<ReportUpvote>(entity =>
        {
            entity.ToTable("ReportUpvotes");
            entity.HasKey(x => new { x.ReportId, x.UserId });
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.Report)
                  .WithMany()
                  .HasForeignKey(x => x.ReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
