using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

/// <summary>
/// Seeds Reports, Issues, and IssueUpdates together in one pass.
/// Each Report → one Issue → one or more IssueUpdates tracking workflow.
/// </summary>
public class SeedIssues
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

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

        var citizens = _db.Users
            .Where(u => _db.UserRoles.Any(ur =>
                _db.Roles.Any(r => r.Name == "Citizen" && r.Id == ur.RoleId) &&
                ur.UserId == u.Id))
            .ToList();

        if (citizens.Count == 0)
        {
            _logger.LogWarning("No citizen users found. Skipping issue seeding.");
            return;
        }

        var issueTypes = _db.IssueTypes.ToList();
        var priorities = _db.IssuePriorities.ToList();
        var statuses = _db.IssueStatuses.ToList();
        var districts = _db.Areas.Where(a => a.AreaType == "District").ToList();
        var wards = _db.Areas.Where(a => a.AreaType == "Ward").ToList();

        if (districts.Count == 0 || wards.Count == 0)
        {
            _logger.LogWarning("Areas not properly seeded. Run SeedAreas first.");
            return;
        }

        var issueData = GetSampleIssues();
        var random = new Random(42);
        var now = DateTime.UtcNow;

        int seeded = 0;

        foreach (var data in issueData)
        {
            var issueType = issueTypes.FirstOrDefault(t => t.TypeCode == data.TypeCode);
            var priority = priorities.FirstOrDefault(p => p.PriorityCode == data.PriorityCode);
            var status = statuses.FirstOrDefault(s => s.StatusCode == data.StatusCode);

            if (issueType == null || priority == null || status == null) continue;

            // Pick district and ward
            var district = districts.First(d => d.AreaCode == data.DistrictCode);
            var districtWards = wards.Where(w => w.ParentAreaId == district.AreaId).ToList();
            var ward = districtWards.Count > 0
                ? districtWards[random.Next(districtWards.Count)]
                : wards[random.Next(wards.Count)];

            // Pick citizen reporter
            var citizen = citizens[random.Next(citizens.Count)];

            // Timing
            var reportedAt = now.AddDays(-random.Next(1, 60));
            var statusCode = data.StatusCode;

            // Resolve/close times based on status
            DateTime? resolvedAt = null;
            DateTime? closedAt = null;
            if (statusCode == "RESOLVED" || statusCode == "CLOSED" || statusCode == "REQUEST_REOPEN")
                resolvedAt = reportedAt.AddDays(random.Next(1, 7));
            if (statusCode == "CLOSED")
                closedAt = resolvedAt?.AddDays(random.Next(1, 3));

            // ====================== Report ======================
            var report = new Report
            {
                PublicCode = $"REP-{now.Year}-{1000 + seeded + 1:D4}",
                ReporterId = citizen.Id,
                AreaId = district.AreaId,
                Title = data.Title,
                Description = data.Description,
                AddressText = data.Address,
                Latitude = (ward.CentroidLatitude ?? district.CentroidLatitude ?? 20.9521m) + (decimal)(random.NextDouble() * 0.002 - 0.001),
                Longitude = (ward.CentroidLongitude ?? district.CentroidLongitude ?? 106.9305m) + (decimal)(random.NextDouble() * 0.002 - 0.001),
                ReportedAt = reportedAt,
                CreatedAt = reportedAt,
                UpdatedAt = reportedAt,
                IsPublic = true,
                IsArchived = false
            };
            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            // ====================== ReportIssueType ======================
            _db.ReportIssueTypes.Add(new ReportIssueType
            {
                ReportId = report.ReportId,
                IssueTypeId = issueType.IssueTypeId,
                IssueTypeName = issueType.TypeName,
                IssueTypeCode = issueType.TypeCode,
                CreatedAt = reportedAt
            });

            // ====================== Issue ======================
            var issue = new Issue
            {
                ReportId = report.ReportId,
                PublicCode = $"ISS-{now.Year}-{1000 + seeded + 1:D4}",
                ReporterId = citizen.Id,
                IssueTypeId = issueType.IssueTypeId,
                AreaId = ward.AreaId,
                PriorityId = priority.PriorityId,
                StatusId = status.StatusId,
                Title = data.Title,
                Description = data.Description,
                AddressText = data.Address,
                Latitude = report.Latitude,
                Longitude = report.Longitude,
                ReportedAt = reportedAt,
                ResolvedAt = resolvedAt,
                ClosedAt = closedAt,
                IsPublic = true,
                IsArchived = false,
                UpvoteCount = random.Next(0, 15)
            };
            _db.Issues.Add(issue);
            await _db.SaveChangesAsync();

            // ====================== IssueUpdates (workflow) ======================
            var updates = BuildWorkflowUpdates(issue, reportedAt, statusCode, random);
            if (updates.Count > 0)
            {
                _db.IssueUpdates.AddRange(updates);
                await _db.SaveChangesAsync();
            }

            seeded++;
        }

        _logger.LogInformation("Seeded {Count} reports, issues, and their updates.", seeded);
    }

    private List<IssueUpdate> BuildWorkflowUpdates(Issue issue, DateTime reportedAt, string finalStatus, Random random)
    {
        var updates = new List<IssueUpdate>();
        var staffUsers = _db.Users
            .Where(u => _db.UserRoles.Any(ur =>
                _db.Roles.Any(r => (r.Name == "DepartmentStaff" || r.Name == "DepartmentManager") && r.Id == ur.RoleId) &&
                ur.UserId == u.Id))
            .ToList();
        var staffId = staffUsers.Count > 0 ? staffUsers[random.Next(staffUsers.Count)].Id : issue.ReporterId;

        // Step 1: System-initiated creation (NEW)
        updates.Add(new IssueUpdate
        {
            IssueId = issue.IssueId,
            CreatedBy = issue.ReporterId,
            FromStatusId = null,
            ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "NEW").StatusId,
            Note = "Công dân gửi phản ánh qua hệ thống.",
            IsSystemGenerated = true,
            CreatedAt = reportedAt
        });

        if (finalStatus == "NEW") return updates;

        // Step 2: NEW → ASSIGNED
        var assignedAt = reportedAt.AddMinutes(random.Next(10, 120));
        updates.Add(new IssueUpdate
        {
            IssueId = issue.IssueId,
            CreatedBy = staffId,
            FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "NEW").StatusId,
            ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "ASSIGNED").StatusId,
            Note = "Hệ thống tự động phân công đơn vị xử lý theo quy tắc định tuyến.",
            IsSystemGenerated = true,
            CreatedAt = assignedAt
        });

        if (finalStatus == "ASSIGNED") return updates;

        // Step 3: ASSIGNED → IN_PROGRESS
        var startedAt = assignedAt.AddMinutes(random.Next(30, 240));
        updates.Add(new IssueUpdate
        {
            IssueId = issue.IssueId,
            CreatedBy = staffId,
            FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "ASSIGNED").StatusId,
            ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
            Note = "Nhân viên tiếp nhận và bắt đầu xử lý sự cố.",
            IsSystemGenerated = false,
            CreatedAt = startedAt
        });

        if (finalStatus == "IN_PROGRESS") return updates;

        // Step 4: IN_PROGRESS → intermediate states
        var progressedAt = startedAt.AddHours(random.Next(1, 12));

        if (finalStatus == "PENDING_INFO")
        {
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = staffId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "PENDING_INFO").StatusId,
                Note = "Cần bổ sung hình ảnh/thông tin từ công dân để xử lý.",
                IsSystemGenerated = false,
                CreatedAt = progressedAt
            });
            return updates;
        }

        if (finalStatus == "REQUEST_REOPEN")
        {
            // IN_PROGRESS → RESOLVED first
            var resolvedAt2 = progressedAt.AddHours(random.Next(1, 8));
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = staffId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "RESOLVED").StatusId,
                Note = "Đơn vị xử lý xác nhận đã hoàn thành công việc.",
                IsSystemGenerated = false,
                CreatedAt = resolvedAt2
            });

            // RESOLVED → REQUEST_REOPEN
            var reopenAt = resolvedAt2.AddHours(random.Next(1, 24));
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = issue.ReporterId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "RESOLVED").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "REQUEST_REOPEN").StatusId,
                Note = "Công dân yêu cầu xử lý lại vì chất lượng không đạt yêu cầu.",
                IsSystemGenerated = false,
                CreatedAt = reopenAt
            });

            // REQUEST_REOPEN → IN_PROGRESS
            var reacceptedAt = reopenAt.AddHours(random.Next(1, 12));
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = staffId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "REQUEST_REOPEN").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
                Note = "Đơn vị xử lý chấp nhận yêu cầu và tiếp tục xử lý.",
                IsSystemGenerated = false,
                CreatedAt = reacceptedAt
            });
            return updates;
        }

        if (finalStatus == "RESOLVED")
        {
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = staffId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "RESOLVED").StatusId,
                Note = "Sự cố đã được xử lý hoàn tất.",
                IsSystemGenerated = false,
                CreatedAt = progressedAt
            });
            return updates;
        }

        if (finalStatus == "CLOSED")
        {
            var resolvedAt3 = progressedAt.AddHours(random.Next(1, 8));
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = staffId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "IN_PROGRESS").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "RESOLVED").StatusId,
                Note = "Đơn vị xử lý xác nhận đã hoàn thành công việc.",
                IsSystemGenerated = false,
                CreatedAt = resolvedAt3
            });

            var closedAt2 = resolvedAt3.AddHours(random.Next(1, 48));
            updates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = issue.ReporterId,
                FromStatusId = _db.IssueStatuses.First(s => s.StatusCode == "RESOLVED").StatusId,
                ToStatusId = _db.IssueStatuses.First(s => s.StatusCode == "CLOSED").StatusId,
                Note = "Công dân xác nhận hài lòng và đóng sự cố.",
                IsSystemGenerated = false,
                CreatedAt = closedAt2
            });
            return updates;
        }

        return updates;
    }

    private static List<SampleIssueData> GetSampleIssues()
    {
        return new List<SampleIssueData>
        {
            // NEW (2)
            new("Đèn đường không sáng tại phường Trần Hưng Đạo", "Đoạn đường Nguyễn Trãi gần ngã tư đèn đường không hoạt động, rất nguy hiểm ban đêm.", "LIGHT", "HIGH", "NEW", "HL", "Đường Nguyễn Trãi, P. Trần Hưng Đạo, TP. Hạ Long"),
            new("Khu vực ngập úng tại phường Vườn Đào", "Mỗi khi mưa to, khu vực phố Vườn Đào bị ngập nước cục bộ, nước đọng không thoát.", "FLOOD", "HIGH", "NEW", "HL", "Phố Vườn Đào, P. Vườn Đào, TP. Hạ Long"),

            // ASSIGNED (3)
            new("Ổ gà lớn trên đường Hà Tuông", "Xuất hiện ổ gà đường kính ~50cm, sâu 15cm. Đã gây ra vụ tai nạn xe máy.", "POTHOLE", "CRITICAL", "ASSIGNED", "HL", "Đường Hà Tuông, P. Hà Tuông, TP. Hạ Long"),
            new("Biển cấm rẽ trái tại ngã tư Hồi Hải mất", "Biển cấm rẽ trái tại ngã tư đã mất 2 tuần, gây nhầm lẫn cho người tham gia giao thông.", "SIGN", "MEDIUM", "ASSIGNED", "HL", "Ngã tư Hồi Hải, P. Hồi Hải, TP. Hạ Long"),
            new("Nắp cống bị mất tại phường Cao Thắng", "Nắp cống thoát nước tại khu vực bị mất, tạo thành hố sâu nguy hiểm cho người đi đường.", "DRAIN", "HIGH", "ASSIGNED", "HL", "P. Cao Thắng, TP. Hạ Long"),

            // IN_PROGRESS (5)
            new("Mặt đường nứt lớn trên đường Vườn Đào", "Mặt đường đoạn qua phố Vườn Đào xuất hiện vết nứt dài 20m, cần sửa chữa trước mùa mưa bão.", "ROAD", "HIGH", "IN_PROGRESS", "HL", "Đường Vườn Đào, P. Vườn Đào, TP. Hạ Long"),
            new("Cây xanh nguy hiểm tại vườn hoa Trần Hưng Đạo", "Một cây bàng lớn có nhiều cành khô sắp gãy, cần cắt tỉa gấp.", "TREE", "MEDIUM", "IN_PROGRESS", "HL", "Vườn hoa Trần Hưng Đạo, P. Trần Hưng Đạo, TP. Hạ Long"),
            new("Điểm tập kết rác bốc mùi hôi tại ngõ 5 Hà Tuông", "Thùng rác tràn ra đường, bốc mùi hôi nồng nặc ảnh hưởng đến sinh hoạt người dân.", "GARBAGE", "LOW", "IN_PROGRESS", "HL", "Ngõ 5 Hà Tuông, P. Hà Tuông, TP. Hạ Long"),
            new("Cống thoát nước bị tắc nghẽn tại phường Cao Thắng", "Cống thoát nước bị tắc rác và bùn, nước không thoát được khi mưa.", "DRAIN", "MEDIUM", "IN_PROGRESS", "HL", "P. Cao Thắng, TP. Hạ Long"),
            new("Đèn LED nhấp nháy liên tục tại đường Hồi Hải", "Trụ đèn cao áp trước số nhà 45 nhấp nháy liên tục 3 ngày, có nguy cơ chập điện.", "LIGHT", "MEDIUM", "IN_PROGRESS", "HL", "Đường Hồi Hải, P. Hồi Hải, TP. Hạ Long"),

            // PENDING_INFO (2)
            new("Mặt đường lún nặng tại đường Trần Hưng Đạo", "Mặt đường dẫn bị lún 2 điểm, gây rung lắc mạnh khi xe qua. Cần bổ sung ảnh chi tiết.", "ROAD", "HIGH", "PENDING_INFO", "HL", "Đường Trần Hưng Đạo, P. Trần Hưng Đạo, TP. Hạ Long"),
            new("Ổ gà nhỏ tại phường Vườn Đào", "Có ổ gà nhỏ trên vỉa hè, cần bổ sung thêm hình ảnh để đánh giá mức độ.", "POTHOLE", "LOW", "PENDING_INFO", "HL", "P. Vườn Đào, TP. Hạ Long"),

            // REQUEST_REOPEN (2)
            new("Vỉa hè sụt lún trước trường học — Yêu cầu xử lý lại", "Sửa chữa vỉa hè trước trường học xong nhưng sau 2 ngày lại sụt tiếp. Công dân yêu cầu xử lý lại.", "POTHOLE", "HIGH", "REQUEST_REOPEN", "HL", "Trước trường THCS Trần Hưng Đạo, P. Trần Hưng Đạo, TP. Hạ Long"),
            new("Biển báo giới hạn tốc độ tại Hà Tuông bị gãy", "Biển giới hạn 40km/h được thay mới nhưng lắp nghiêng, cần điều chỉnh lại.", "SIGN", "LOW", "REQUEST_REOPEN", "HL", "Đường Hà Tuông, P. Hà Tuông, TP. Hạ Long"),

            // RESOLVED (2)
            new("Rác thải xây dựng đổ trộm tại bãi đất trống Hồi Hải", "Lượng lớn rác thải xây dựng (gạch, bê tông) đã được thu gom và xử lý.", "GARBAGE", "MEDIUM", "RESOLVED", "HL", "Bãi đất trống Hồi Hải, P. Hồi Hải, TP. Hạ Long"),
            new("Cây phong nghiêng về đường điện cao áp tại Cao Thắng", "Cây phong cao 8m đã được cắt tỉa an toàn, không còn nguy hiểm.", "TREE", "HIGH", "RESOLVED", "HL", "Khu vực phố Cao Thắng, P. Cao Thắng, TP. Hạ Long"),

            // CLOSED (2)
            new("Đèn đường cao áp không sáng tại phường Vườn Đào", "Trụ đèn đã được thay bóng mới, hoạt động bình thường. Công dân xác nhận hài lòng.", "LIGHT", "HIGH", "CLOSED", "HL", "Đường Vườn Đào, P. Vườn Đào, TP. Hạ Long"),
            new("Ổ gà nhỏ trên vỉa hè Hà Tuông", "Vỉa hè đã được sửa chữa và san phẳng. Công dân xác nhận hài lòng.", "POTHOLE", "MEDIUM", "CLOSED", "HL", "Vỉa hè đường Hà Tuông, P. Hà Tuông, TP. Hạ Long"),

            // Uông Bí district
            new("Ngập úng khu vực phường Vất Tân khi mưa lớn", "Mỗi trận mưa lớn, khu vực phường Vất Tân ngập nước cục bộ 30cm.", "FLOOD", "HIGH", "IN_PROGRESS", "UB", "P. Vất Tân, TP. Uông Bí, Quảng Ninh"),
            new("Biển báo giao thông tại ngã tư Quang Trung bị mất", "Biển cấm rẽ phải tại ngã tư Quang Trung đã mất 1 tuần.", "SIGN", "MEDIUM", "ASSIGNED", "UB", "Ngã tư Quang Trung, P. Quang Trung, TP. Uông Bí"),

            // Cẩm Phả district
            new("Mặt đường nhựa bị nứt nghiêm trọng tại Cẩm Mỹ Trung", "Vết nứt dài 30m trên mặt đường nhựa, có nguy cơ mở rộng.", "ROAD", "HIGH", "IN_PROGRESS", "CP", "Đường Cẩm Mỹ Trung, P. Cẩm Mỹ Trung, TP. Cẩm Phả"),
            new("Điểm tập kết rác bốc mùi tại Cẩm Đông", "Thùng rác tại ngõ 3 Cẩm Đông tràn ra đường.", "GARBAGE", "LOW", "NEW", "CP", "Ngõ 3 Cẩm Đông, P. Cẩm Đông, TP. Cẩm Phả"),

            // Móng Cái district
            new("Nắp cống bị vỡ tại phường Ka Long", "Miệng cống thoát nước bị vỡ, tạo hố sâu nguy hiểm cho người đi đường.", "DRAIN", "CRITICAL", "ASSIGNED", "MC", "P. Ka Long, TP. Móng Cái, Quảng Ninh"),
            new("Đèn đường không hoạt động tại phường Ninh Mỹ", "Trụ đèn cao áp trước số nhà 20 không sáng suốt 1 tuần.", "LIGHT", "MEDIUM", "IN_PROGRESS", "MC", "P. Ninh Mỹ, TP. Móng Cái, Quảng Ninh"),
        };
    }

    private record SampleIssueData(
        string Title,
        string Description,
        string TypeCode,
        string PriorityCode,
        string StatusCode,
        string DistrictCode,
        string Address
    );
}
