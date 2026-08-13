using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssues
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;
    private const decimal TargetLat = 20.460213m;
    private const decimal TargetLon = 106.138710m;

    public SeedIssues(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.Issues.Any())
        {
            _logger.LogInformation("Issues already exist, skipping.");
            return;
        }

        var adminUser = _db.Users.FirstOrDefault(u => u.Email == "admin@urbaninfra.vn");
        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found. Skipping issue seeding.");
            return;
        }

        var issueTypes = _db.IssueTypes.ToList();
        var priorities = _db.IssuePriorities.ToList();
        var statuses = _db.IssueStatuses.ToList();
        var areas = _db.Areas.ToList();

        if (issueTypes.Count == 0 || priorities.Count == 0 || statuses.Count == 0 || areas.Count == 0)
        {
            _logger.LogWarning("Required reference data missing. Run other seeders first.");
            return;
        }

        var haLongArea = areas.FirstOrDefault(a => a.AreaCode == "HL");
        var issues = GenerateSampleIssues(adminUser.Id, issueTypes, priorities, statuses, areas);

        _db.Issues.AddRange(issues);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} issues near ({Lat}, {Lon}).", issues.Count, TargetLat, TargetLon);
    }

    private List<Issue> GenerateSampleIssues(
        string reporterId,
        List<IssueType> issueTypes,
        List<IssuePriority> priorities,
        List<IssueStatus> statuses,
        List<Area> areas)
    {
        var issueData = new List<(string Title, string Desc, string TypeCode, string PriorityCode, string StatusCode, decimal Lat, decimal Lon, string Address)>
        {
            (
                "Đèn đường không sáng tại đường Trần Hưng Đạo",
                "Từ ngày 10/08, đoạn đường Trần Hưng Đạo gần cầu Bãi Cháy đèn đường không hoạt động, rất nguy hiểm vào ban đêm.",
                "LIGHT", "HIGH", "IN_PROGRESS",
                20.4560m, 106.1350m,
                "Đường Trần Hưng Đạo, TP. Hạ Long"
            ),
            (
                "Ổ gà lớn trên đường Vườn Đào",
                "Xuất hiện ổ gà có đường kính khoảng 50cm, sâu 15cm trên mặt đường. Đã gây ra 1 vụ tai nạn xe máy.",
                "POTHOLE", "CRITICAL", "ASSIGNED",
                20.4555m, 106.1385m,
                "Đường Vườn Đào, Phường Vườn Đào, TP. Hạ Long"
            ),
            (
                "Khu vực ngập úng tại Bãi Cháy Đầm Hà",
                "Mỗi khi mưa to, khu vực Bãi Cháy Đầm Hà bị ngập nước cục bộ, nước đọng không thoát được.",
                "FLOOD", "HIGH", "NEW",
                20.4625m, 106.1345m,
                "Khu vực Bãi Cháy Đầm Hà, TP. Hạ Long"
            ),
            (
                "Biển báo giao thông bị mất tại ngã tư Hồng Gai",
                "Biển cấm rẽ trái tại ngã tư Hồng Gai đã bị mất từ 2 tuần trước, gây nhầm lẫn cho người tham gia giao thông.",
                "SIGN", "MEDIUM", "PENDING_INFO",
                20.4580m, 106.1370m,
                "Ngã tư Hồng Gai, TP. Hạ Long"
            ),
            (
                "Mặt đường nứt lớn trên đường Bãi Cháy",
                "Mặt đường quốc lộ 18 đoạn qua Bãi Cháy xuất hiện vết nứt dài 20m, cần sửa chữa trước mùa mưa bão.",
                "ROAD", "HIGH", "IN_PROGRESS",
                20.4530m, 106.1390m,
                "Đường Bãi Cháy, TP. Hạ Long"
            ),
            (
                "Nắp cống bị mất tại khu vực Hà Tuông",
                "Nắp cống thoát nước tại khu vực Hà Tuông bị mất, tạo thành hố sâu nguy hiểm cho người đi đường.",
                "DRAIN", "HIGH", "ASSIGNED",
                20.4590m, 106.1365m,
                "Khu vực Hà Tuông, TP. Hạ Long"
            ),
            (
                "Cây xanh nguy hiểm tại công viên Bãi Cháy",
                "Một cây bàng lớn trong công viên Bãi Cháy có nhiều cành khô sắp gãy, cần cắt tỉa gấp.",
                "TREE", "MEDIUM", "NEW",
                20.4545m, 106.1380m,
                "Công viên Bãi Cháy, TP. Hạ Long"
            ),
            (
                "Điểm tập kết rác bốc mùi hôi tại Cao Thắng",
                "Thùng rác tại ngõ 5 đường Cao Thắng tràn ra đường, bốc mùi hôi nồng nặc ảnh hưởng đến sinh hoạt người dân.",
                "GARBAGE", "LOW", "NEW",
                20.4550m, 106.1375m,
                "Đường Cao Thắng, Phường Cao Thắng, TP. Hạ Long"
            ),
            (
                "Đèn đường nhấp nháy liên tục tại Hồi Hải Bãi Cháy",
                "Trụ đèn cao áp trước số nhà 45 đường Hồi Hải nhấp nháy liên tục 3 ngày nay, có nguy cơ chập điện.",
                "LIGHT", "MEDIUM", "NEW",
                20.4515m, 106.1395m,
                "Đường Hồi Hải Bãi Cháy, TP. Hạ Long"
            ),
            (
                "Vỉa hè sụt lún trước trường học Vườn Đào",
                "Vỉa hè trước cổng trường Tiểu học Vườn Đào bị sụt lún 1 đoạn 3m, gây nguy hiểm cho học sinh.",
                "POTHOLE", "HIGH", "RESOLVED",
                20.4570m, 106.1380m,
                "Trước trường Tiểu học Vườn Đào, TP. Hạ Long"
            ),
            (
                "Biển báo giới hạn tốc độ bị gãy tại quốc lộ 18",
                "Biển giới hạn tốc độ 40km/h tại km15 quốc lộ 18 bị gãy chân, nghiêng sang một bên.",
                "SIGN", "LOW", "IN_PROGRESS",
                20.4605m, 106.1360m,
                "Quốc lộ 18, TP. Hạ Long"
            ),
            (
                "Mặt đường lún nặng tại cầu Bãi Cháy",
                "Mặt đường dẫn lên cầu Bãi Cháy bị lún 2 điểm, gây rung lắc mạnh khi xe qua.",
                "ROAD", "HIGH", "ASSIGNED",
                20.4595m, 106.1355m,
                "Đường dẫn cầu Bãi Cháy, TP. Hạ Long"
            ),
            (
                "Cống thoát nước bị tắc nghẽn tại Trần Hưng Đạo",
                "Cống thoát nước trên đường Trần Hưng Đạo bị tắc rác và bùn, nước không thoát được khi mưa.",
                "DRAIN", "MEDIUM", "NEW",
                20.4610m, 106.1340m,
                "Đường Trần Hưng Đạo, TP. Hạ Long"
            ),
            (
                "Cây phong lá đỏ nghiêng về đường điện cao áp",
                "Cây phong lá đỏ cao 8m nghiêng về phía đường dây điện, cần xử lý trước mùa mưa bão.",
                "TREE", "HIGH", "NEW",
                20.4630m, 106.1370m,
                "Khu vực Bãi Cháy Đầm Hà, TP. Hạ Long"
            ),
            (
                "Rác thải xây dựng đổ trộm tại bãi đất trống Vườn Đào",
                "Một lượng lớn rác thải xây dựng (gạch, bê tông) bị đổ trộm tại bãi đất trống gần vườn hoa Vườn Đào.",
                "GARBAGE", "MEDIUM", "ASSIGNED",
                20.4640m, 106.1385m,
                "Bãi đất trống Vườn Đào, TP. Hạ Long"
            )
        };

        var existingCodes = _db.Issues.Select(i => i.PublicCode).ToHashSet();
        var haLongArea = areas.FirstOrDefault(a => a.AreaCode == "HL") ?? areas.First();

        var issues = new List<Issue>();
        var random = new Random(42);
        var now = DateTime.UtcNow;

        foreach (var (title, desc, typeCode, priorityCode, statusCode, lat, lon, address) in issueData)
        {
            var issueType = issueTypes.FirstOrDefault(t => t.TypeCode == typeCode);
            var priority = priorities.FirstOrDefault(p => p.PriorityCode == priorityCode);
            var status = statuses.FirstOrDefault(s => s.StatusCode == statusCode);

            if (issueType == null || priority == null || status == null) continue;

            var code = GeneratePublicCode(existingCodes);
            existingCodes.Add(code);

            var reportedAt = now.AddDays(-random.Next(1, 30));
            var resolvedAt = status.StatusCode == "RESOLVED" || status.StatusCode == "CLOSED"
                ? reportedAt.AddDays(random.Next(1, 7))
                : (DateTime?)null;
            var closedAt = status.StatusCode == "CLOSED" ? resolvedAt?.AddDays(random.Next(1, 3)) : null;

            issues.Add(new Issue
            {
                PublicCode = code,
                ReporterId = reporterId,
                IssueTypeId = issueType.IssueTypeId,
                AreaId = haLongArea.AreaId,
                PriorityId = priority.PriorityId,
                StatusId = status.StatusId,
                Title = title,
                Description = desc,
                AddressText = address,
                Latitude = lat,
                Longitude = lon,
                ReportedAt = reportedAt,
                ResolvedAt = resolvedAt,
                ClosedAt = closedAt,
                IsPublic = true,
                UpvoteCount = random.Next(0, 15)
            });
        }

        return issues;
    }

    private static string GeneratePublicCode(HashSet<string> existing)
    {
        var year = DateTime.UtcNow.Year;
        var counter = 1;
        string code;
        do
        {
            code = $"ISS-{year}-{counter:D6}";
            counter++;
        } while (existing.Contains(code));

        return code;
    }
}
