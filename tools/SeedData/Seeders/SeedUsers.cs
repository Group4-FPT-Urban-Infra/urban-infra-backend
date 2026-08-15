using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedUsers
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger _logger;

    public SeedUsers(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // 1. Ensure roles exist
        await EnsureRoleAsync("Citizen");
        await EnsureRoleAsync("DepartmentStaff");
        await EnsureRoleAsync("DepartmentManager");
        await EnsureRoleAsync("Admin");

        // 2. Seed users per role
        await SeedAdminsAsync();
        await SeedCitizensAsync();
        await SeedDepartmentStaffAndManagersAsync();
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName)) return;

        var role = new ApplicationRole();
        role.Name = roleName;
        role.Description = $"Role: {roleName}";
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create role {Role}: {Errors}",
                roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        else
        {
            _logger.LogInformation("Created role: {Role}", roleName);
        }
    }

    private async Task SeedAdminsAsync()
    {
        var adminEmail = "admin@urbaninfra.vn";
        if (_db.Users.Any(u => u.Email == adminEmail))
        {
            _logger.LogInformation("Admin user already exists, skipping.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Quản trị viên hệ thống",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(admin, "Admin@123456");
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(admin, "Admin");
        _logger.LogInformation("Created admin user: {Email}", adminEmail);
    }

    private async Task SeedCitizensAsync()
    {
        var citizens = new[]
        {
            ("minh.nguyen@example.com", "Nguyễn Văn Minh", "Citizen"),
            ("lan.pham@example.com", "Phạm Thị Lan", "Citizen"),
            ("duc.tran@example.com", "Trần Đức Đức", "Citizen"),
            ("hoa.le@example.com", "Lê Thị Hòa", "Citizen"),
            ("nam.vo@example.com", "Võ Nam", "Citizen"),
        };

        var password = "Citizen@123456";
        int created = 0;

        foreach (var (email, fullName, role) in citizens)
        {
            if (_db.Users.Any(u => u.Email == email))
            {
                _logger.LogInformation("Citizen {Email} already exists, skipping.", email);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create citizen {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await _userManager.AddToRoleAsync(user, role);
            created++;
        }

        _logger.LogInformation("Seeded {Count} citizen users.", created);
    }

    private async Task SeedDepartmentStaffAndManagersAsync()
    {
        var departments = _db.Departments.ToList();
        if (departments.Count == 0)
        {
            _logger.LogWarning("No departments found. Run SeedDepartments first.");
            return;
        }

        var password = "Staff@123456";
        int staffCreated = 0;
        int managerCreated = 0;

        foreach (var dept in departments)
        {
            // Create manager
            var managerEmail = $"{dept.DepartmentCode.ToLower()}.manager@urbaninfra.vn";
            if (!_db.Users.Any(u => u.Email == managerEmail))
            {
                var manager = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = $"Trưởng phòng {dept.DepartmentName}",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(manager, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(manager, "DepartmentManager");

                    // Add as department member with IsManager = true
                    _db.DepartmentMembers.Add(new DepartmentMember
                    {
                        DepartmentId = dept.DepartmentId,
                        UserId = manager.Id,
                        JobTitle = $"Trưởng phòng {dept.DepartmentName}",
                        IsManager = true,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                    await _db.SaveChangesAsync();
                    managerCreated++;
                    _logger.LogInformation("Created manager: {Email} for dept {Dept}", managerEmail, dept.DepartmentCode);
                }
                else
                {
                    _logger.LogWarning("Failed to create manager {Email}: {Errors}",
                        managerEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Create 2 staff members per department
            for (int i = 1; i <= 2; i++)
            {
                var staffEmail = $"{dept.DepartmentCode.ToLower()}.staff{i}@urbaninfra.vn";
                if (_db.Users.Any(u => u.Email == staffEmail))
                {
                    _logger.LogInformation("Staff {Email} already exists, skipping.", staffEmail);
                    continue;
                }

                var staff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = $"Nhân viên {i} - {dept.DepartmentName}",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(staff, password);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to create staff {Email}: {Errors}",
                        staffEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await _userManager.AddToRoleAsync(staff, "DepartmentStaff");

                // Add as department member with IsManager = false
                _db.DepartmentMembers.Add(new DepartmentMember
                {
                    DepartmentId = dept.DepartmentId,
                    UserId = staff.Id,
                    JobTitle = $"Cán bộ {dept.DepartmentName}",
                    IsManager = false,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
                staffCreated++;
            }
        }

        _logger.LogInformation("Seeded {StaffCount} staff and {ManagerCount} managers.",
            staffCreated, managerCreated);
    }
}
