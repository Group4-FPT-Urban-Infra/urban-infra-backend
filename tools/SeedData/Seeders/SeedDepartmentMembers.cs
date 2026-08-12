using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedDepartmentMembers
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedDepartmentMembers(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.DepartmentMembers.Any())
        {
            _logger.LogInformation("DepartmentMembers already exist, skipping.");
            return;
        }

        var departments = _db.Departments.ToList();
        var users = _db.Users.ToList();

        if (departments.Count == 0)
        {
            _logger.LogWarning("No departments found. Run SeedDepartments first.");
            return;
        }

        var staffUsers = users.Where(u => u.Email != "admin@urbaninfra.vn").ToList();

        if (staffUsers.Count == 0)
        {
            _logger.LogWarning("No staff users found. Skipping department members seeding.");
            return;
        }

        var members = new List<DepartmentMember>();

        foreach (var dept in departments)
        {
            var deptMembers = staffUsers.Take(2).ToList();
            foreach (var user in deptMembers)
            {
                members.Add(new DepartmentMember
                {
                    DepartmentId = dept.DepartmentId,
                    UserId = user.Id,
                    JobTitle = GetJobTitle(dept.DepartmentCode),
                    IsManager = members.All(m => m.DepartmentId != dept.DepartmentId),
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            staffUsers = staffUsers.Skip(2).ToList();
        }

        _db.DepartmentMembers.AddRange(members);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} department members.", members.Count);
    }

    private static string GetJobTitle(string deptCode) => deptCode switch
    {
        "QLDT" => "Cán bộ Quản lý Đô thị",
        "CSGT" => "Kỹ sư Thoát nước",
        "MT" => "Cán bộ Môi trường",
        "GT" => "Cán bộ Giao thông",
        "VSDN" => "Công nhân Vệ sinh",
        _ => "Nhân viên"
    };
}
