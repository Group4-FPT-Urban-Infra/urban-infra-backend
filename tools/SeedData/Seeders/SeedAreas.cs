using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedAreas
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedAreas(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.Areas.Any())
        {
            _logger.LogInformation("Areas already exist, skipping.");
            return;
        }

        // ============================================================
        // Province
        // ============================================================
        var quangNinh = new Area
        {
            AreaCode = "QN",
            AreaName = "Tỉnh Quảng Ninh",
            AreaType = "Province",
            CentroidLatitude = 21.2753m,
            CentroidLongitude = 106.9672m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ============================================================
        // Districts (AreaType = "District") under Quang Ninh
        // ============================================================
        var haLong = new Area
        {
            AreaCode = "HL",
            AreaName = "Thành phố Hạ Long",
            AreaType = "District",
            CentroidLatitude = 20.9521m,
            CentroidLongitude = 106.9305m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        quangNinh.SubAreas.Add(haLong);
        haLong.ParentAreaId = quangNinh.AreaId;

        var uongBi = new Area
        {
            AreaCode = "UB",
            AreaName = "Thành phố Uông Bí",
            AreaType = "District",
            CentroidLatitude = 21.0367m,
            CentroidLongitude = 106.7681m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        quangNinh.SubAreas.Add(uongBi);
        uongBi.ParentAreaId = quangNinh.AreaId;

        var camPha = new Area
        {
            AreaCode = "CP",
            AreaName = "Thành phố Cẩm Phả",
            AreaType = "District",
            CentroidLatitude = 21.0219m,
            CentroidLongitude = 107.0483m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        quangNinh.SubAreas.Add(camPha);
        camPha.ParentAreaId = quangNinh.AreaId;

        var mongCai = new Area
        {
            AreaCode = "MC",
            AreaName = "Thành phố Móng Cái",
            AreaType = "District",
            CentroidLatitude = 21.5254m,
            CentroidLongitude = 107.9273m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        quangNinh.SubAreas.Add(mongCai);
        mongCai.ParentAreaId = quangNinh.AreaId;

        // ============================================================
        // Wards (AreaType = "Ward") under Hạ Long
        // ============================================================
        var wardsHL = new[]
        {
            ("THD", "Phường Trần Hưng Đạo",   20.9610m, 106.9952m),
            ("CT",  "Phường Cao Thắng",        20.9550m, 106.9920m),
            ("HT",  "Phường Hà Tuông",         20.9480m, 106.9600m),
            ("HBC", "Phường Hồi Hải",          20.9510m, 106.9580m),
            ("VD",  "Phường Vườn Đào",         20.9460m, 106.9400m),
        };

        foreach (var (code, name, lat, lon) in wardsHL)
        {
            var ward = new Area
            {
                AreaCode = code,
                AreaName = name,
                AreaType = "Ward",
                CentroidLatitude = lat,
                CentroidLongitude = lon,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            haLong.SubAreas.Add(ward);
            ward.ParentAreaId = haLong.AreaId;
        }

        // ============================================================
        // Wards under Uông Bí
        // ============================================================
        var wardsUB = new[]
        {
            ("VP",  "Phường Vất Tân",           21.0380m, 106.7650m),
            ("YTH", "Phường Yết Thượng",       21.0420m, 106.7700m),
            ("QL",  "Phường Quang Trung",       21.0330m, 106.7620m),
        };

        foreach (var (code, name, lat, lon) in wardsUB)
        {
            var ward = new Area
            {
                AreaCode = code,
                AreaName = name,
                AreaType = "Ward",
                CentroidLatitude = lat,
                CentroidLongitude = lon,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            uongBi.SubAreas.Add(ward);
            ward.ParentAreaId = uongBi.AreaId;
        }

        // ============================================================
        // Wards under Cẩm Phả
        // ============================================================
        var wardsCP = new[]
        {
            ("CMT", "Phường Cẩm Mỹ Trung",    21.0180m, 107.0520m),
            ("CQ",  "Phường Cẩm Quý",         21.0250m, 107.0440m),
            ("CD",  "Phường Cẩm Đông",         21.0120m, 107.0550m),
        };

        foreach (var (code, name, lat, lon) in wardsCP)
        {
            var ward = new Area
            {
                AreaCode = code,
                AreaName = name,
                AreaType = "Ward",
                CentroidLatitude = lat,
                CentroidLongitude = lon,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            camPha.SubAreas.Add(ward);
            ward.ParentAreaId = camPha.AreaId;
        }

        // ============================================================
        // Wards under Móng Cái
        // ============================================================
        var wardsMC = new[]
        {
            ("KH",  "Phường Ka Long",           21.5280m, 107.9220m),
            ("NMC", "Phường Ninh Mỹ",           21.5200m, 107.9300m),
            ("TM",  "Phường Trà Cổ",           21.5100m, 107.9350m),
        };

        foreach (var (code, name, lat, lon) in wardsMC)
        {
            var ward = new Area
            {
                AreaCode = code,
                AreaName = name,
                AreaType = "Ward",
                CentroidLatitude = lat,
                CentroidLongitude = lon,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            mongCai.SubAreas.Add(ward);
            ward.ParentAreaId = mongCai.AreaId;
        }

        _db.Areas.Add(quangNinh);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} areas (1 province, 4 districts, 14 wards).", _db.Areas.Count());
    }
}
