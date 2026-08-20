using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssueStatuses
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedIssueStatuses(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Use upsert logic so this works even if statuses already exist
        // and we want to ensure REQUEST_REOPEN is present
        var statuses = new List<IssueStatus>
        {
            new() { StatusCode = "NEW",             StatusName = "Mới tiếp nhận",        IsClosed = false, IsPublicVisible = true,  DisplayOrder = 1, IsActive = true },
            new() { StatusCode = "ASSIGNED",        StatusName = "Đã phân công",          IsClosed = false, IsPublicVisible = true,  DisplayOrder = 2, IsActive = true },
            new() { StatusCode = "IN_PROGRESS",     StatusName = "Đang xử lý",           IsClosed = false, IsPublicVisible = true,  DisplayOrder = 3, IsActive = true },
            new() { StatusCode = "PENDING_INFO",     StatusName = "Chờ bổ sung thông tin", IsClosed = false, IsPublicVisible = true,  DisplayOrder = 4, IsActive = true },
            new() { StatusCode = "REQUEST_REOPEN",  StatusName = "Yêu cầu xử lý lại",   IsClosed = false, IsPublicVisible = true,  DisplayOrder = 5, IsActive = true },
            new() { StatusCode = "RESOLVED",        StatusName = "Đã xử lý",             IsClosed = true,  IsPublicVisible = true,  DisplayOrder = 6, IsActive = true },
            new() { StatusCode = "CLOSED",           StatusName = "Đã đóng",               IsClosed = true,  IsPublicVisible = true,  DisplayOrder = 7, IsActive = true },
            new() { StatusCode = "REJECTED",         StatusName = "Từ chối",              IsClosed = true,  IsPublicVisible = false, DisplayOrder = 8, IsActive = true },
        };

        foreach (var status in statuses)
        {
            var existing = _db.IssueStatuses.FirstOrDefault(s => s.StatusCode == status.StatusCode);
            if (existing == null)
            {
                _db.IssueStatuses.Add(status);
            }
            else
            {
                existing.StatusName = status.StatusName;
                existing.IsClosed = status.IsClosed;
                existing.IsPublicVisible = status.IsPublicVisible;
                existing.DisplayOrder = status.DisplayOrder;
                existing.IsActive = status.IsActive;
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded/upserted {Count} issue statuses.", statuses.Count);
    }
}
