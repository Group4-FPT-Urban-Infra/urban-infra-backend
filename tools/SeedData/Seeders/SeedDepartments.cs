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

        // ============================================================
        // Level 1: Root department (Sở Xây dựng)
        // ============================================================
        var soXayDung = new Department
        {
            DepartmentCode = "SXD",
            DepartmentName = "Sở Xây dựng tỉnh Quảng Ninh",
            Email = "sxd@quangninh.gov.vn",
            Phone = "0203.382.1111",
            Address = "Số 1 Đường Trần Hưng Đạo, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(soXayDung);
        await _db.SaveChangesAsync();

        // ============================================================
        // Level 2: Child departments under Sở Xây dựng
        // ============================================================
        var qldt = new Department
        {
            DepartmentCode = "QLDT",
            DepartmentName = "Phòng Quản lý Đô thị",
            Email = "qldt@quangninh.gov.vn",
            Phone = "0203.384.1234",
            Address = "Số 10 Đường Trần Hưng Đạo, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(qldt);
        qldt.ParentDepartmentId = soXayDung.DepartmentId;

        var thoatNuoc = new Department
        {
            DepartmentCode = "TN",
            DepartmentName = "Công ty Thoát nước và Xử lý nước thải",
            Email = "thoatnuoc@quangninh.gov.vn",
            Phone = "0203.384.5678",
            Address = "Số 5 Đường Bãi Cháy, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(thoatNuoc);
        thoatNuoc.ParentDepartmentId = soXayDung.DepartmentId;

        var moiTruong = new Department
        {
            DepartmentCode = "MT",
            DepartmentName = "Công ty Môi trường Đô thị",
            Email = "moitruong@quangninh.gov.vn",
            Phone = "0203.384.9012",
            Address = "Số 10 Đường Hồng Gai, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(moiTruong);
        moiTruong.ParentDepartmentId = soXayDung.DepartmentId;

        var giaoThong = new Department
        {
            DepartmentCode = "GT",
            DepartmentName = "Công ty Quản lý Giao thông công cộng",
            Email = "giaothong@quangninh.gov.vn",
            Phone = "0203.384.3456",
            Address = "Số 3 Đường Vườn Đào, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(giaoThong);
        giaoThong.ParentDepartmentId = soXayDung.DepartmentId;

        var veSinh = new Department
        {
            DepartmentCode = "VSDN",
            DepartmentName = "Công ty Vệ sinh Đô thị",
            Email = "vesinh@quangninh.gov.vn",
            Phone = "0203.384.7890",
            Address = "Số 7 Đường Cao Thắng, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(veSinh);
        veSinh.ParentDepartmentId = soXayDung.DepartmentId;

        var mongCai = new Department
        {
            DepartmentCode = "MC",
            DepartmentName = "Đội Thanh tra Móng Cái",
            Email = "mongcai@quangninh.gov.vn",
            Phone = "0203.389.0001",
            Address = "Số 2 Đường Ka Long, TP. Móng Cái, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        soXayDung.ChildDepartments.Add(mongCai);
        mongCai.ParentDepartmentId = soXayDung.DepartmentId;

        _db.Departments.AddRange(qldt, thoatNuoc, moiTruong, giaoThong, veSinh, mongCai);
        await _db.SaveChangesAsync();

        // ============================================================
        // Level 3: Grandchild departments under QLDT
        // ============================================================
        var csgtHL = new Department
        {
            DepartmentCode = "CSGT",
            DepartmentName = "Đội Cảnh sát Giao thông Hạ Long",
            Email = "csgt.halong@quangninh.gov.vn",
            Phone = "0203.385.1234",
            Address = "Số 15 Đường Trần Hưng Đạo, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        qldt.ChildDepartments.Add(csgtHL);
        csgtHL.ParentDepartmentId = qldt.DepartmentId;

        var duaThoatNuoc = new Department
        {
            DepartmentCode = "DTN",
            DepartmentName = "Đội Dẫn thoát nước Hạ Long",
            Email = "dtn@quangninh.gov.vn",
            Phone = "0203.385.5678",
            Address = "Số 20 Đường Hà Tuông, TP. Hạ Long, Quảng Ninh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        thoatNuoc.ChildDepartments.Add(duaThoatNuoc);
        duaThoatNuoc.ParentDepartmentId = thoatNuoc.DepartmentId;

        _db.Departments.AddRange(csgtHL, duaThoatNuoc);
        await _db.SaveChangesAsync();

        var count = _db.Departments.Count();
        _logger.LogInformation("Seeded {Count} departments (1 root + 6 level-2 + 2 level-3).", count);
    }
}
