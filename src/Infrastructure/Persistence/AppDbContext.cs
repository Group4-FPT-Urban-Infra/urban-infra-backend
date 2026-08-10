using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;



namespace UrbanInfraSystem.Infrastructure.Persistence;

/// <summary>
/// DbContext gốc, kế thừa IdentityDbContext để có sẵn bảng Users/Roles/Claims của
/// ASP.NET Core Identity. Các entity nghiệp vụ (Issue, Department, Category...)
/// sẽ được thêm DbSet ở Sprint 2 khi xây module CRUD chính.
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Đổi tên bảng Identity mặc định cho gọn (tuỳ chọn).
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

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

        builder.Entity<Area>(entity =>
        {
            entity.ToTable("Areas");
            entity.HasKey(a => a.AreaId);

            entity.Property(a => a.AreaCode)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(a => a.AreaName)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(a => a.AreaType)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(a => a.Boundary)
                  .HasColumnType("geography");

            entity.Property(a => a.CentroidLatitude)
                  .HasPrecision(9, 6);

            entity.Property(a => a.CentroidLongitude)
                  .HasPrecision(9, 6);

            entity.HasOne(a => a.ParentArea)
                  .WithMany(a => a.SubAreas)
                  .HasForeignKey(a => a.ParentAreaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // IssueType configuration
        builder.Entity<IssueType>(entity =>
        {
            entity.ToTable("IssueTypes");
            
            // Primary key
            entity.HasKey(it => it.IssueTypeId);
            entity.Property(it => it.IssueTypeId)
                  .ValueGeneratedOnAdd();

            // Required fields
            entity.Property(it => it.TypeCode)
                  .IsRequired()
                  .HasMaxLength(30);
            
            entity.Property(it => it.TypeName)
                  .IsRequired()
                  .HasMaxLength(150)
                  .IsUnicode(true); // NVARCHAR

            entity.Property(it => it.IconUrl)
                  .HasMaxLength(1000)
                  .IsUnicode(true);

            entity.Property(it => it.Description)
                  .HasMaxLength(2000)
                  .IsUnicode(true);

            // Unique index on TypeCode
            entity.HasIndex(it => it.TypeCode)
                  .IsUnique()
                  .HasDatabaseName("IX_IssueTypes_TypeCode");

            // Self-referencing relationship (parent-child hierarchy)
            entity.HasOne(it => it.ParentIssueType)
                  .WithMany(it => it.SubIssueTypes)
                  .HasForeignKey(it => it.ParentIssueTypeId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid orphan issues

            // Index for parent lookup
            entity.HasIndex(it => it.ParentIssueTypeId)
                  .HasDatabaseName("IX_IssueTypes_ParentIssueTypeId");

            // Index for active filter
            entity.HasIndex(it => it.IsActive)
                  .HasDatabaseName("IX_IssueTypes_IsActive");
        });

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
    }
}
