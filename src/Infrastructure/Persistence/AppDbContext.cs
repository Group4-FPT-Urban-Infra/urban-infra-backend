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
    }
}
