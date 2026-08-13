using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssueTypes
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedIssueTypes(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.IssueTypes.Any())
        {
            _logger.LogInformation("IssueTypes already exist, skipping.");
            return;
        }

        var types = new List<IssueType>
        {
            new()
            {
                TypeCode = "LIGHT",
                TypeName = "Đèn đường hỏng",
                Description = "Đèn chiếu sáng công cộng bị hỏng, nhấp nháy hoặc không hoạt động",
                IconUrl = "/icons/light.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "POTHOLE",
                TypeName = "Ổ gà / Vỉa hè hỏng",
                Description = "Ổ gà, vỉa hè sụt lún, gạch vỡ trên đường phố",
                IconUrl = "/icons/pothole.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "FLOOD",
                TypeName = "Ngập úng đô thị",
                Description = "Khu vực ngập nước khi mưa lớn, thoát nước kém",
                IconUrl = "/icons/flood.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "SIGN",
                TypeName = "Biển báo giao thông",
                Description = "Biển báo giao thông bị hỏng, mất, hoặc che khuất",
                IconUrl = "/icons/sign.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "ROAD",
                TypeName = "Hư hỏng mặt đường",
                Description = "Mặt đường bị nứt, lún, hoặc hư hỏng nghiêm trọng",
                IconUrl = "/icons/road.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "DRAIN",
                TypeName = "Cống rãnh thoát nước",
                Description = "Cống thoát nước bị tắc, vỡ hoặc thiếu nắp",
                IconUrl = "/icons/drain.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "TREE",
                TypeName = "Cây xanh công trình",
                Description = "Cây xanh nguy hiểm, cành gãy, cần cắt tỉa hoặc trồng mới",
                IconUrl = "/icons/tree.svg",
                IsActive = true
            },
            new()
            {
                TypeCode = "GARBAGE",
                TypeName = "Rác thải đô thị",
                Description = "Điểm tập kết rác không được xử lý, bốc mùi hôi",
                IconUrl = "/icons/garbage.svg",
                IsActive = true
            }
        };

        _db.IssueTypes.AddRange(types);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} issue types.", types.Count);
    }
}
