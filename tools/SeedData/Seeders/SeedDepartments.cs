using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedDepartments
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedDepartments(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.Departments.Any())
        {
            _logger.LogInformation("Departments already exist, skipping.");
            return;
        }

        var departments = new List<Department>
        {
            new()
            {
                DepartmentCode = "QLDT",
                DepartmentName = "Phòng Quản lý Đô thị",
                Email = "qldt@urbaninfra.vn",
                Phone = "0203.384.1234",
                Address = "Số 1 Đường Trần Hưng Đạo, TP. Hạ Long, Quảng Ninh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                DepartmentCode = "CSGT",
                DepartmentName = "Công ty Thoát nước và Xử lý nước thải",
                Email = "csgt@urbaninfra.vn",
                Phone = "0203.384.5678",
                Address = "Số 5 Đường Bãi Cháy, TP. Hạ Long, Quảng Ninh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                DepartmentCode = "MT",
                DepartmentName = "Công ty Môi trường Đô thị",
                Email = "mt@urbaninfra.vn",
                Phone = "0203.384.9012",
                Address = "Số 10 Đường Hồng Gai, TP. Hạ Long, Quảng Ninh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                DepartmentCode = "GT",
                DepartmentName = "Công ty Quản lý Giao thông công cộng",
                Email = "gt@urbaninfra.vn",
                Phone = "0203.384.3456",
                Address = "Số 3 Đường Vườn Đào, TP. Hạ Long, Quảng Ninh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                DepartmentCode = "VSDN",
                DepartmentName = "Công ty Vệ sinh Đô thị",
                Email = "vsdn@urbaninfra.vn",
                Phone = "0203.384.7890",
                Address = "Số 7 Đường Cao Thắng, TP. Hạ Long, Quảng Ninh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _db.Departments.AddRange(departments);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} departments.", departments.Count);
    }
}
