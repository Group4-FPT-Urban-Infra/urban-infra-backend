using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
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

        var targetLat = 20.460213m;
        var targetLon = 106.138710m;

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

        var haLong = new Area
        {
            AreaCode = "HL",
            AreaName = "Thành phố Hạ Long",
            AreaType = "City",
            CentroidLatitude = targetLat,
            CentroidLongitude = targetLon,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        quangNinh.SubAreas.Add(haLong);
        haLong.ParentAreaId = quangNinh.AreaId;

        var hongGai = new Area
        {
            AreaCode = "HG",
            AreaName = "Quận Hồng Gai",
            AreaType = "District",
            CentroidLatitude = 20.9527m,
            CentroidLongitude = 106.9981m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        haLong.SubAreas.Add(hongGai);
        hongGai.ParentAreaId = haLong.AreaId;

        var baiChay = new Area
        {
            AreaCode = "BC",
            AreaName = "Quận Bãi Cháy",
            AreaType = "District",
            CentroidLatitude = 20.9501m,
            CentroidLongitude = 106.9634m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        haLong.SubAreas.Add(baiChay);
        baiChay.ParentAreaId = haLong.AreaId;

        var tranHungDao = new Area
        {
            AreaCode = "THD",
            AreaName = "Phường Trần Hưng Đạo",
            AreaType = "Ward",
            CentroidLatitude = 20.9610m,
            CentroidLongitude = 106.9952m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        hongGai.SubAreas.Add(tranHungDao);
        tranHungDao.ParentAreaId = hongGai.AreaId;

        var caoThang = new Area
        {
            AreaCode = "CT",
            AreaName = "Phường Cao Thắng",
            AreaType = "Ward",
            CentroidLatitude = 20.9550m,
            CentroidLongitude = 106.9920m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        hongGai.SubAreas.Add(caoThang);
        caoThang.ParentAreaId = hongGai.AreaId;

        var haTuong = new Area
        {
            AreaCode = "HT",
            AreaName = "Phường Hà Tuông",
            AreaType = "Ward",
            CentroidLatitude = 20.9480m,
            CentroidLongitude = 106.9600m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        baiChay.SubAreas.Add(haTuong);
        haTuong.ParentAreaId = baiChay.AreaId;

        var hoiBaiChay = new Area
        {
            AreaCode = "HBC",
            AreaName = "Phường Hồi Hải Bãi Cháy",
            AreaType = "Ward",
            CentroidLatitude = 20.9510m,
            CentroidLongitude = 106.9580m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        baiChay.SubAreas.Add(hoiBaiChay);
        hoiBaiChay.ParentAreaId = baiChay.AreaId;

        var vuonDao = new Area
        {
            AreaCode = "VD",
            AreaName = "Phường Vườn Đào",
            AreaType = "Ward",
            CentroidLatitude = 20.4560m,
            CentroidLongitude = 106.1380m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        haLong.SubAreas.Add(vuonDao);
        vuonDao.ParentAreaId = haLong.AreaId;

        var bachDam = new Area
        {
            AreaCode = "BD",
            AreaName = "Phường Bãi Chály Đầm Hà",
            AreaType = "Ward",
            CentroidLatitude = 20.4630m,
            CentroidLongitude = 106.1350m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        haLong.SubAreas.Add(bachDam);
        bachDam.ParentAreaId = haLong.AreaId;

        _db.Areas.Add(quangNinh);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} areas.", _db.Areas.Count());
    }
}
