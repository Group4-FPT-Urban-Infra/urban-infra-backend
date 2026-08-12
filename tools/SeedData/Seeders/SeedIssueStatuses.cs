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
        if (_db.IssueStatuses.Any())
        {
            _logger.LogInformation("IssueStatuses already exist, skipping.");
            return;
        }

        var statuses = new List<IssueStatus>
        {
            new()
            {
                StatusCode = "NEW",
                StatusName = "Mới tiếp nhận",
                IsClosed = false,
                IsPublicVisible = true,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                StatusCode = "ASSIGNED",
                StatusName = "Đã phân công",
                IsClosed = false,
                IsPublicVisible = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                StatusCode = "IN_PROGRESS",
                StatusName = "Đang xử lý",
                IsClosed = false,
                IsPublicVisible = true,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                StatusCode = "PENDING_INFO",
                StatusName = "Chờ bổ sung thông tin",
                IsClosed = false,
                IsPublicVisible = true,
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                StatusCode = "RESOLVED",
                StatusName = "Đã xử lý",
                IsClosed = false,
                IsPublicVisible = true,
                DisplayOrder = 5,
                IsActive = true
            },
            new()
            {
                StatusCode = "CLOSED",
                StatusName = "Đã đóng",
                IsClosed = true,
                IsPublicVisible = true,
                DisplayOrder = 6,
                IsActive = true
            },
            new()
            {
                StatusCode = "REJECTED",
                StatusName = "Từ chối",
                IsClosed = true,
                IsPublicVisible = false,
                DisplayOrder = 7,
                IsActive = true
            }
        };

        _db.IssueStatuses.AddRange(statuses);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} issue statuses.", statuses.Count);
    }
}
