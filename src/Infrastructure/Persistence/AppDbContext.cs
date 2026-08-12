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
    public DbSet<IssueUpvote> IssueUpvotes => Set<IssueUpvote>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueAttachment> IssueAttachments => Set<IssueAttachment>();
    public DbSet<IssueUpdate> IssueUpdates => Set<IssueUpdate>();
    public DbSet<IssueSla> IssueSlas => Set<IssueSla>();

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

        // 4. IssueTypes (Giữ lại cấu hình phân cấp chi tiết của dev)
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

        // 9. SlaPolicies (Gộp cấu hình tính năng SLA của bạn)
        builder.Entity<SlaPolicy>(entity =>
        {
            entity.ToTable("SlaPolicies");
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

            // Đảm bảo mỗi cặp IssueType + Priority chỉ có 1 chính sách SLA
            entity.HasIndex(s => new { s.IssueTypeId, s.PriorityId }).IsUnique();

            // Lọc các bản ghi xóa mềm (Soft-delete filter)
            entity.HasQueryFilter(s => !s.IsDeleted);

            // Constraint kiểm tra thời gian dương
            entity.HasCheckConstraint("CK_SlaPolicies_Minutes_Positive", "ResolutionMinutes > 0 AND FirstResponseMinutes > 0");
        });

        // 10. IssueUpvotes
        builder.Entity<IssueUpvote>(entity =>
        {
            entity.ToTable("IssueUpvotes");
            entity.HasKey(u => new { u.IssueId, u.UserId });

            entity.Property(u => u.CreatedAt).HasColumnType("datetime2(0)");

            entity.HasOne(u => u.Issue)
                  .WithMany(i => i.Upvotes)
                  .HasForeignKey(u => u.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(u => u.UserId).HasDatabaseName("IX_IssueUpvotes_UserId");
        });

        // 11. Issues
        builder.Entity<Issue>(entity =>
        {
            entity.ToTable("Issues");
            entity.HasKey(i => i.IssueId);
            entity.Property(i => i.IssueId).ValueGeneratedOnAdd();

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

        // 10. RoutingRules
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

        // 14. IssueSlas
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
    }
}
