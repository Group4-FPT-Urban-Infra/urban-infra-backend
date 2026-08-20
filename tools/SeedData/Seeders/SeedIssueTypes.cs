using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
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

        // ============================================================
        // Root Issue Types
        // ============================================================
        var light = new IssueType
        {
            TypeCode = "LIGHT",
            TypeName = "Đèn đường hỏng",
            Description = "Đèn chiếu sáng công cộng bị hỏng, nhấp nháy hoặc không hoạt động",
            IconUrl = "/icons/light.svg",
            IsActive = true
        };
        var pothole = new IssueType
        {
            TypeCode = "POTHOLE",
            TypeName = "Ổ gà / Vỉa hè hỏng",
            Description = "Ổ gà, vỉa hè sụt lún, gạch vỡ trên đường phố",
            IconUrl = "/icons/pothole.svg",
            IsActive = true
        };
        var flood = new IssueType
        {
            TypeCode = "FLOOD",
            TypeName = "Ngập úng đô thị",
            Description = "Khu vực ngập nước khi mưa lớn, thoát nước kém",
            IconUrl = "/icons/flood.svg",
            IsActive = true
        };
        var sign = new IssueType
        {
            TypeCode = "SIGN",
            TypeName = "Biển báo giao thông",
            Description = "Biển báo giao thông bị hỏng, mất, hoặc che khuất",
            IconUrl = "/icons/sign.svg",
            IsActive = true
        };
        var road = new IssueType
        {
            TypeCode = "ROAD",
            TypeName = "Hư hỏng mặt đường",
            Description = "Mặt đường bị nứt, lún, hoặc hư hỏng nghiêm trọng",
            IconUrl = "/icons/road.svg",
            IsActive = true
        };
        var drain = new IssueType
        {
            TypeCode = "DRAIN",
            TypeName = "Cống rãnh thoát nước",
            Description = "Cống thoát nước bị tắc, vỡ hoặc thiếu nắp",
            IconUrl = "/icons/drain.svg",
            IsActive = true
        };
        var tree = new IssueType
        {
            TypeCode = "TREE",
            TypeName = "Cây xanh công trình",
            Description = "Cây xanh nguy hiểm, cành gãy, cần cắt tỉa hoặc trồng mới",
            IconUrl = "/icons/tree.svg",
            IsActive = true
        };
        var garbage = new IssueType
        {
            TypeCode = "GARBAGE",
            TypeName = "Rác thải đô thị",
            Description = "Điểm tập kết rác không được xử lý, bốc mùi hôi",
            IconUrl = "/icons/garbage.svg",
            IsActive = true
        };

        var rootTypes = new List<IssueType> { light, pothole, flood, sign, road, drain, tree, garbage };
        _db.IssueTypes.AddRange(rootTypes);
        await _db.SaveChangesAsync();

        // ============================================================
        // Child Issue Types
        // ============================================================

        // LIGHT children
        light.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "LED",
            TypeName = "Đèn LED",
            Description = "Đèn chiếu sáng LED công nghệ mới",
            IconUrl = "/icons/led.svg",
            IsActive = true
        });
        light.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "SODIUM",
            TypeName = "Đèn cao áp Sodium",
            Description = "Đèn cao áp hơi Natri (Sodium) truyền thống",
            IconUrl = "/icons/sodium.svg",
            IsActive = true
        });
        light.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "SOLAR",
            TypeName = "Đèn năng lượng mặt trời",
            Description = "Đèn chiếu sáng sử dụng pin năng lượng mặt trời",
            IconUrl = "/icons/solar.svg",
            IsActive = true
        });

        // POTHOLE children
        pothole.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "SMALLPOT",
            TypeName = "Ổ gà nhỏ (< 20cm)",
            Description = "Ổ gà có đường kính nhỏ, độ sâu dưới 5cm",
            IconUrl = "/icons/smallpothole.svg",
            IsActive = true
        });
        pothole.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "BIGPOT",
            TypeName = "Ổ gà lớn (> 20cm)",
            Description = "Ổ gà có đường kính lớn, độ sâu trên 5cm, nguy hiểm",
            IconUrl = "/icons/bigpothole.svg",
            IsActive = true
        });
        pothole.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "SIDEWALK",
            TypeName = "Vỉa hè hỏng",
            Description = "Vỉa hè bị sụt lún, gạch vỡ, gốc cây phá hoại",
            IconUrl = "/icons/sidewalk.svg",
            IsActive = true
        });

        // FLOOD children
        flood.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "DRAIN_FLOOD",
            TypeName = "Ngập do thoát nước kém",
            Description = "Khu vực ngập do cống thoát nước không hoạt động",
            IconUrl = "/icons/drainflood.svg",
            IsActive = true
        });
        flood.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "RAIN_FLOOD",
            TypeName = "Ngập do mưa lớn",
            Description = "Khu vực ngập cục bộ khi mưa lớn, cơ sở hạ tầng đáp ứng không kịp",
            IconUrl = "/icons/rainflood.svg",
            IsActive = true
        });

        // SIGN children
        sign.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "TRAFFIC",
            TypeName = "Biển báo giao thông",
            Description = "Biển cấm, biển chỉ dẫn, biển cảnh báo bị hỏng/mất",
            IconUrl = "/icons/traffic.svg",
            IsActive = true
        });
        sign.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "WARNING",
            TypeName = "Biển báo nguy hiểm",
            Description = "Biển cảnh báo nguy hiểm, biển phụ",
            IconUrl = "/icons/warning.svg",
            IsActive = true
        });

        // ROAD children
        road.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "ASPHALT",
            TypeName = "Mặt đường nhựa",
            Description = "Đường nhựa bị nứt, lún, ổ gà trên nền nhựa",
            IconUrl = "/icons/asphalt.svg",
            IsActive = true
        });
        road.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "CONCRETE",
            TypeName = "Mặt đường bê tông",
            Description = "Đường bê tông xi măng bị nứt, lún, vỡ",
            IconUrl = "/icons/concrete.svg",
            IsActive = true
        });
        road.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "BRIDGE",
            TypeName = "Cầu và gầm cầu",
            Description = "Hư hỏng trên mặt cầu, gầm cầu, khe co giãn",
            IconUrl = "/icons/bridge.svg",
            IsActive = true
        });

        // DRAIN children
        drain.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "MANHOLE",
            TypeName = "Miệng cống / Hố ga",
            Description = "Miệng cống thoát nước bị vỡ, mất nắp, tắc nghẽn",
            IconUrl = "/icons/manhole.svg",
            IsActive = true
        });
        drain.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "GRATE",
            TypeName = "Song chắn rác",
            Description = "Song chắn rác bị tắc, vỡ, hoặc thiếu",
            IconUrl = "/icons/grate.svg",
            IsActive = true
        });
        drain.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "CHANNEL",
            TypeName = "Mương/ống dẫn nước",
            Description = "Mương thoát nước, ống dẫn bị tắc, sạt lở, vỡ",
            IconUrl = "/icons/channel.svg",
            IsActive = true
        });

        // TREE children
        tree.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "TRIM",
            TypeName = "Cắt tỉa cây xanh",
            Description = "Cây xanh cần cắt tỉa cành khô, cành che khuất biển báo",
            IconUrl = "/icons/trim.svg",
            IsActive = true
        });
        tree.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "REMOVE",
            TypeName = "Chặt / Di dời cây",
            Description = "Cây nguy hiểm cần chặt bỏ hoặc di dời",
            IconUrl = "/icons/remove.svg",
            IsActive = true
        });
        tree.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "NEW_TREE",
            TypeName = "Trồng cây mới",
            Description = "Vị trí cần trồng cây xanh mới, thay thế cây đã chặt",
            IconUrl = "/icons/newtree.svg",
            IsActive = true
        });

        // GARBAGE children
        garbage.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "COLLECT",
            TypeName = "Thu gom rác thải",
            Description = "Điểm tập kết rác cần được thu gom vệ sinh",
            IconUrl = "/icons/collect.svg",
            IsActive = true
        });
        garbage.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "ILLEGAL",
            TypeName = "Đổ trộm rác thải",
            Description = "Đổ trộm rác xây dựng, rác thải sinh hoạt bất hợp pháp",
            IconUrl = "/icons/illegal.svg",
            IsActive = true
        });
        garbage.SubIssueTypes.Add(new IssueType
        {
            TypeCode = "HAZARD",
            TypeName = "Chất thải nguy hại",
            Description = "Chất thải nguy hại (pin, sơn, dầu nhớt) cần xử lý đặc biệt",
            IconUrl = "/icons/hazard.svg",
            IsActive = true
        });

        _db.IssueTypes.AddRange(light.SubIssueTypes);
        _db.IssueTypes.AddRange(pothole.SubIssueTypes);
        _db.IssueTypes.AddRange(flood.SubIssueTypes);
        _db.IssueTypes.AddRange(sign.SubIssueTypes);
        _db.IssueTypes.AddRange(road.SubIssueTypes);
        _db.IssueTypes.AddRange(drain.SubIssueTypes);
        _db.IssueTypes.AddRange(tree.SubIssueTypes);
        _db.IssueTypes.AddRange(garbage.SubIssueTypes);
        await _db.SaveChangesAsync();

        var total = _db.IssueTypes.Count();
        var roots = rootTypes.Count;
        var children = total - roots;
        _logger.LogInformation("Seeded {Total} issue types ({Roots} root + {Children} children).", total, roots, children);
    }
}
