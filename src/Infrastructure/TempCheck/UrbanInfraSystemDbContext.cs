using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class UrbanInfraSystemDbContext : DbContext
{
    public UrbanInfraSystemDbContext()
    {
    }

    public UrbanInfraSystemDbContext(DbContextOptions<UrbanInfraSystemDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DepartmentMember> DepartmentMembers { get; set; }

    public virtual DbSet<EscalationEvent> EscalationEvents { get; set; }

    public virtual DbSet<EscalationRule> EscalationRules { get; set; }

    public virtual DbSet<Issue> Issues { get; set; }

    public virtual DbSet<IssuePriority> IssuePriorities { get; set; }

    public virtual DbSet<IssueSla> IssueSlas { get; set; }

    public virtual DbSet<IssueStatus> IssueStatuses { get; set; }

    public virtual DbSet<IssueType> IssueTypes { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleClaim> RoleClaims { get; set; }

    public virtual DbSet<RoutingRule> RoutingRules { get; set; }

    public virtual DbSet<SlaPolicy> SlaPolicies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserClaim> UserClaims { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UrbanInfraSystemDb;Trusted_Connection=True;TrustServerCertificate=True", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasIndex(e => e.ParentAreaId, "IX_Areas_ParentAreaId");

            entity.Property(e => e.AreaCode).HasMaxLength(30);
            entity.Property(e => e.AreaName).HasMaxLength(150);
            entity.Property(e => e.AreaType).HasMaxLength(30);
            entity.Property(e => e.CentroidLatitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.CentroidLongitude).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.ParentArea).WithMany(p => p.InverseParentArea).HasForeignKey(d => d.ParentAreaId);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.DepartmentCode, "IX_Departments_DepartmentCode").IsUnique();

            entity.HasIndex(e => e.ParentDepartmentId, "IX_Departments_ParentDepartmentId");

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DepartmentName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ParentDepartment).WithMany(p => p.InverseParentDepartment).HasForeignKey(d => d.ParentDepartmentId);
        });

        modelBuilder.Entity<DepartmentMember>(entity =>
        {
            entity.HasKey(e => new { e.DepartmentId, e.UserId });

            entity.HasIndex(e => e.UserId, "IX_DepartmentMembers_UserId");

            entity.Property(e => e.JobTitle).HasMaxLength(120);
            entity.Property(e => e.JoinedAt).HasPrecision(0);
            entity.Property(e => e.LeftAt).HasPrecision(0);

            entity.HasOne(d => d.Department).WithMany(p => p.DepartmentMembers)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.DepartmentMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<EscalationEvent>(entity =>
        {
            entity.HasIndex(e => e.EscalationRuleId, "IX_EscalationEvents_EscalationRuleId");

            entity.HasIndex(e => e.IssueId, "IX_EscalationEvents_IssueId");

            entity.HasIndex(e => e.TargetDepartmentId, "IX_EscalationEvents_TargetDepartmentId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(450);
            entity.Property(e => e.EventStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.TargetUserId).HasMaxLength(450);

            entity.HasOne(d => d.EscalationRule).WithMany(p => p.EscalationEvents).HasForeignKey(d => d.EscalationRuleId);

            entity.HasOne(d => d.Issue).WithMany(p => p.EscalationEvents)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TargetDepartment).WithMany(p => p.EscalationEvents).HasForeignKey(d => d.TargetDepartmentId);
        });

        modelBuilder.Entity<EscalationRule>(entity =>
        {
            entity.HasIndex(e => e.TargetDepartmentId, "IX_EscalationRules_TargetDepartmentId");

            entity.HasIndex(e => new { e.SlaPolicyId, e.EscalationLevel, e.OverdueMinutes, e.TargetDepartmentId, e.TargetRoleName }, "UX_EscalationRules_UniqueCombination")
                .IsUnique()
                .HasFilter("([TargetDepartmentId] IS NOT NULL AND [TargetRoleName] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.NotificationTemplate).HasMaxLength(4000);
            entity.Property(e => e.NotificationTitle).HasMaxLength(250);
            entity.Property(e => e.TargetRoleName)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.SlaPolicy).WithMany(p => p.EscalationRules).HasForeignKey(d => d.SlaPolicyId);

            entity.HasOne(d => d.TargetDepartment).WithMany(p => p.EscalationRules).HasForeignKey(d => d.TargetDepartmentId);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasIndex(e => e.IssueTypeId, "IX_Issues_IssueTypeId");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_Issues_LatLon");

            entity.HasIndex(e => e.PriorityId, "IX_Issues_PriorityId");

            entity.HasIndex(e => e.StatusId, "IX_Issues_StatusId");

            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PublicCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReportedAt).HasPrecision(0);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.IssueType).WithMany(p => p.Issues)
                .HasForeignKey(d => d.IssueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Priority).WithMany(p => p.Issues).HasForeignKey(d => d.PriorityId);

            entity.HasOne(d => d.Status).WithMany(p => p.Issues).HasForeignKey(d => d.StatusId);
        });

        modelBuilder.Entity<IssuePriority>(entity =>
        {
            entity.HasKey(e => e.PriorityId);

            entity.HasIndex(e => e.PriorityCode, "IX_IssuePriorities_PriorityCode").IsUnique();

            entity.HasIndex(e => e.SeverityRank, "IX_IssuePriorities_SeverityRank").IsUnique();

            entity.Property(e => e.PriorityCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PriorityName).HasMaxLength(50);
        });

        modelBuilder.Entity<IssueSla>(entity =>
        {
            entity.HasIndex(e => e.IssueId, "IX_IssueSlas_IssueId");

            entity.HasIndex(e => e.SlaPolicyId, "IX_IssueSlas_SlaPolicyId");

            entity.HasOne(d => d.Issue).WithMany(p => p.IssueSlas).HasForeignKey(d => d.IssueId);
        });

        modelBuilder.Entity<IssueStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.HasIndex(e => e.DisplayOrder, "IX_IssueStatuses_DisplayOrder").IsUnique();

            entity.HasIndex(e => e.StatusCode, "IX_IssueStatuses_StatusCode").IsUnique();

            entity.Property(e => e.StatusCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StatusName).HasMaxLength(80);
        });

        modelBuilder.Entity<IssueType>(entity =>
        {
            entity.HasKey(e => e.IssueTypeId);

            entity.HasIndex(e => e.IsActive, "IX_IssueTypes_IsActive");

            entity.HasIndex(e => e.ParentIssueTypeId, "IX_IssueTypes_ParentIssueTypeId");

            entity.HasIndex(e => e.TypeCode, "IX_IssueTypes_TypeCode").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.IconUrl).HasMaxLength(1000);
            entity.Property(e => e.TypeCode).HasMaxLength(30);
            entity.Property(e => e.TypeName).HasMaxLength(150);

            entity.HasOne(d => d.ParentIssueType).WithMany(p => p.InverseParentIssueType).HasForeignKey(d => d.ParentIssueTypeId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token, "IX_RefreshTokens_Token").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_RefreshTokens_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Token).HasMaxLength(512);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_RoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<RoutingRule>(entity =>
        {
            entity.HasIndex(e => new { e.AreaId, e.IssueTypeId }, "IX_RoutingRules_AreaId_IssueTypeId").IsUnique();

            entity.HasIndex(e => e.DepartmentId, "IX_RoutingRules_DepartmentId");

            entity.HasIndex(e => e.IssueTypeId, "IX_RoutingRules_IssueTypeId");

            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Area).WithMany(p => p.RoutingRules)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.RoutingRules)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IssueType).WithMany(p => p.RoutingRules)
                .HasForeignKey(d => d.IssueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.HasIndex(e => new { e.IssueTypeId, e.PriorityId }, "IX_SlaPolicies_IssueTypeId_PriorityId").IsUnique();

            entity.HasIndex(e => e.PriorityId, "IX_SlaPolicies_PriorityId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.IssueType).WithMany(p => p.SlaPolicies)
                .HasForeignKey(d => d.IssueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Priority).WithMany(p => p.SlaPolicies)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_UserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
