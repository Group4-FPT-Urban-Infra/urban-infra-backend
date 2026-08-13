using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueService : IIssueService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IssueService> _logger;

    public IssueService(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<IssueService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ApiResponse<IssueDetailResponse>> CreateIssueAsync(
        CreateIssueFormRequest request,
        string reporterId,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra tài khoản công dân
        var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == reporterId, cancellationToken);
        if (reporter == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Tài khoản công dân không tồn tại."
            };
        }

        // 2. Validate Loại sự cố
        var issueType = await _context.IssueTypes
            .FirstOrDefaultAsync(t => t.IssueTypeId == request.IssueTypeId && t.IsActive, cancellationToken);
        if (issueType == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = $"Loại sự cố (ID: {request.IssueTypeId}) không tồn tại hoặc đã bị vô hiệu hóa."
            };
        }

        // 3. Validate Khu vực
        var area = await _context.Areas
            .FirstOrDefaultAsync(a => a.AreaId == request.AreaId && a.IsActive, cancellationToken);
        if (area == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = $"Khu vực (ID: {request.AreaId}) không tồn tại hoặc đã bị vô hiệu hóa."
            };
        }

        // 4. Validate hoặc gán Mức độ ưu tiên mặc định
        IssuePriority? priority = null;
        if (request.PriorityId.HasValue)
        {
            priority = await _context.IssuePriorities
                .FirstOrDefaultAsync(p => p.PriorityId == request.PriorityId.Value && p.IsActive, cancellationToken);
            if (priority == null)
            {
                return new ApiResponse<IssueDetailResponse>
                {
                    Success = false,
                    Message = $"Mức độ ưu tiên (ID: {request.PriorityId.Value}) không tồn tại hoặc đã bị vô hiệu hóa."
                };
            }
        }
        else
        {
            priority = await _context.IssuePriorities
                .Where(p => p.IsActive)
                .OrderBy(p => p.SeverityRank)
                .FirstOrDefaultAsync(cancellationToken);

            if (priority == null)
            {
                return new ApiResponse<IssueDetailResponse>
                {
                    Success = false,
                    Message = "Hệ thống chưa cấu hình mức độ ưu tiên mặc định."
                };
            }
        }

        // 5. Tìm trạng thái khởi tạo mặc định (ví dụ: REPORTED hoặc trạng thái đầu tiên)
        var status = await _context.IssueStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (status == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Hệ thống chưa cấu hình trạng thái sự cố."
            };
        }

        // 6. Sinh PublicCode duy nhất
        string publicCode;
        do
        {
            var randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            publicCode = $"ISS-{DateTime.UtcNow:yyyyMMdd}-{randomSuffix}";
        }
        while (await _context.Issues.AnyAsync(i => i.PublicCode == publicCode, cancellationToken));

        // 7. Tạo Issue, đính kèm, timeline và assignment trong cùng transaction DB.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var issue = new Issue
        {
            PublicCode = publicCode,
            ReporterId = reporterId,
            IssueTypeId = issueType.IssueTypeId,
            AreaId = area.AreaId,
            PriorityId = priority.PriorityId,
            StatusId = status.StatusId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AddressText = request.AddressText?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            UpvoteCount = 0,
            IsPublic = true,
            ReportedAt = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Tự động tra cứu SLA Policy theo type + priority và tạo bản ghi IssueSla
        var slaPolicy = await _context.SlaPolicies
            .FirstOrDefaultAsync(s => s.IssueTypeId == issue.IssueTypeId && s.PriorityId == issue.PriorityId, cancellationToken);

        int firstResponseMinutes = slaPolicy?.FirstResponseMinutes ?? 120;
        int resolutionMinutes = slaPolicy?.ResolutionMinutes ?? 1440;

        var issueSla = new IssueSla
        {
            IssueId = issue.IssueId,
            SlaPolicyId = slaPolicy?.Id,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            FirstResponseDueAt = issue.ReportedAt.AddMinutes(firstResponseMinutes),
            ResolutionDueAt = issue.ReportedAt.AddMinutes(resolutionMinutes),
            CreatedAt = DateTime.UtcNow
        };

        _context.IssueSlas.Add(issueSla);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Lưu đính kèm hình ảnh (nếu có)
        if (request.Images != null && request.Images.Count > 0)
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads", "issues");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var image in request.Images)
            {
                if (image.Length == 0) continue;

                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream, cancellationToken);
                }

                var fileUrl = $"/uploads/issues/{uniqueFileName}";
                var attachment = new IssueAttachment
                {
                    IssueId = issue.IssueId,
                    UploadedBy = reporterId,
                    Kind = "image",
                    FileUrl = fileUrl,
                    ThumbnailUrl = fileUrl,
                    MimeType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType,
                    FileSizeBytes = image.Length,
                    CreatedAt = DateTime.UtcNow
                };

                _context.IssueAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // 9. Ghi nhận timeline khởi tạo (IssueUpdate)
        var initialUpdate = new IssueUpdate
        {
            IssueId = issue.IssueId,
            FromStatusId = null,
            ToStatusId = status.StatusId,
            Note = "Báo cáo sự cố đã được tạo thành công.",
            CreatedBy = reporterId,
            IsSystemGenerated = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.IssueUpdates.Add(initialUpdate);

        // 10. Tự động định tuyến theo cặp loại sự cố + khu vực (nếu có rule active).
        var routingRule = await _context.RoutingRules
            .Include(r => r.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IssueTypeId == issue.IssueTypeId &&
                                      r.AreaId == issue.AreaId &&
                                      r.IsActive &&
                                      r.Department.IsActive,
                cancellationToken);

        if (routingRule is not null)
        {
            _context.IssueAssignments.Add(new IssueAssignment
            {
                IssueId = issue.IssueId,
                DepartmentId = routingRule.DepartmentId,
                RoutingRuleId = routingRule.RoutingRuleId,
                AssignmentMethod = "AUTO",
                AssignmentNote = "Tự động định tuyến theo loại sự cố và khu vực.",
                AssignedAt = DateTime.UtcNow,
                IsCurrent = true
            });
            _context.IssueUpdates.Add(new IssueUpdate
            {
                IssueId = issue.IssueId,
                FromStatusId = status.StatusId,
                ToStatusId = status.StatusId,
                Note = $"Đã định tuyến tự động đến đơn vị '{routingRule.Department.DepartmentName}'.",
                CreatedBy = reporterId,
                IsSystemGenerated = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // 11. Trả về chi tiết sự cố vừa tạo
        var detailResult = await GetIssueByIdAsync(issue.IssueId, reporterId, cancellationToken);
        return new ApiResponse<IssueDetailResponse>
        {
            Success = true,
            Message = "Tạo báo cáo sự cố thành công.",
            Data = detailResult.Data
        };
    }

    public async Task<ApiResponse<IssueDetailResponse>> GetIssueByIdAsync(
        long issueId,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Sla)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = $"Không tìm thấy báo cáo sự cố có ID = {issueId}"
            };
        }

        var reporter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == issue.ReporterId, cancellationToken);

        bool hasUpvoted = false;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            hasUpvoted = await _context.IssueUpvotes
                .AnyAsync(u => u.IssueId == issueId && u.UserId == currentUserId, cancellationToken);
        }

        var response = MapToDetailResponse(issue, reporter?.FullName ?? "Công dân", hasUpvoted);

        var currentDepartment = await _context.IssueAssignments
            .Where(x => x.IssueId == issueId && x.IsCurrent)
            .Select(x => new LookupItemResponse
            {
                Id = x.DepartmentId,
                Name = x.Department.DepartmentName,
                Code = x.Department.DepartmentCode
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        response.CurrentDepartment = currentDepartment;

        return new ApiResponse<IssueDetailResponse>
        {
            Success = true,
            Data = response
        };
    }

    public async Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchIssuesAsync(
        SearchIssuesRequest request,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Where(i => i.IsPublic)
            .AsNoTracking();

        if (request.IssueTypeId.HasValue)
        {
            query = query.Where(i => i.IssueTypeId == request.IssueTypeId.Value);
        }

        if (request.AreaId.HasValue)
        {
            query = query.Where(i => i.AreaId == request.AreaId.Value);
        }

        if (request.StatusCodes != null && request.StatusCodes.Length > 0)
        {
            var codes = request.StatusCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Status.StatusCode.ToUpper()));
        }

        if (request.PriorityCodes != null && request.PriorityCodes.Length > 0)
        {
            var codes = request.PriorityCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Priority.PriorityCode.ToUpper()));
        }

        if (request.From.HasValue)
        {
            query = query.Where(i => i.ReportedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(i => i.ReportedAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(keyword) ||
                                     i.Description.ToLower().Contains(keyword) ||
                                     i.PublicCode.ToLower().Contains(keyword));
        }

        if (request.MinLatitude.HasValue) query = query.Where(i => i.Latitude >= request.MinLatitude.Value);
        if (request.MaxLatitude.HasValue) query = query.Where(i => i.Latitude <= request.MaxLatitude.Value);
        if (request.MinLongitude.HasValue) query = query.Where(i => i.Longitude >= request.MinLongitude.Value);
        if (request.MaxLongitude.HasValue) query = query.Where(i => i.Longitude <= request.MaxLongitude.Value);

        var totalItems = await query.LongCountAsync(cancellationToken);

        query = request.Sort?.ToLower() switch
        {
            "reportedatasc" => query.OrderBy(i => i.ReportedAt),
            "upvotedesc" => query.OrderByDescending(i => i.UpvoteCount),
            _ => query.OrderByDescending(i => i.ReportedAt)
        };

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var issues = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        HashSet<long> upvotedIssueIds = [];
        if (!string.IsNullOrEmpty(currentUserId) && issues.Count > 0)
        {
            var issueIds = issues.Select(i => i.IssueId).ToList();
            var upvotes = await _context.IssueUpvotes
                .Where(u => u.UserId == currentUserId && issueIds.Contains(u.IssueId))
                .Select(u => u.IssueId)
                .ToListAsync(cancellationToken);
            upvotedIssueIds = [.. upvotes];
        }

        var items = issues.Select(i => MapToSummaryResponse(i, upvotedIssueIds.Contains(i.IssueId))).ToList();

        return new ApiResponse<PagedResponse<IssueSummaryResponse>>
        {
            Success = true,
            Data = new PagedResponse<IssueSummaryResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            }
        };
    }

    public async Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMyIssuesAsync(
        GetMyIssuesRequest request,
        string reporterId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Where(i => i.ReporterId == reporterId)
            .AsNoTracking();

        if (request.StatusCodes != null && request.StatusCodes.Length > 0)
        {
            var codes = request.StatusCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Status.StatusCode.ToUpper()));
        }

        var totalItems = await query.LongCountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var issues = await query
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        HashSet<long> upvotedIssueIds = [];
        if (issues.Count > 0)
        {
            var issueIds = issues.Select(i => i.IssueId).ToList();
            var upvotes = await _context.IssueUpvotes
                .Where(u => u.UserId == reporterId && issueIds.Contains(u.IssueId))
                .Select(u => u.IssueId)
                .ToListAsync(cancellationToken);
            upvotedIssueIds = [.. upvotes];
        }

        var items = issues.Select(i => MapToSummaryResponse(i, upvotedIssueIds.Contains(i.IssueId))).ToList();

        return new ApiResponse<PagedResponse<IssueSummaryResponse>>
        {
            Success = true,
            Data = new PagedResponse<IssueSummaryResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            }
        };
    }

    public async Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyIssuesAsync(
        FindNearbyIssuesRequest request,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddDays(-request.WithinDays);

        var issues = await _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Where(i => i.IssueTypeId == request.IssueTypeId && i.ReportedAt >= fromDate && i.IsPublic)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var nearbyList = new List<NearbyIssueResponse>();

        foreach (var issue in issues)
        {
            var distance = CalculateHaversineDistance(
                (double)request.Latitude, (double)request.Longitude,
                (double)issue.Latitude, (double)issue.Longitude);

            if (distance <= request.RadiusMeters)
            {
                bool hasUpvoted = false;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    hasUpvoted = await _context.IssueUpvotes
                        .AnyAsync(u => u.IssueId == issue.IssueId && u.UserId == currentUserId, cancellationToken);
                }

                var summary = MapToSummaryResponse(issue, hasUpvoted);
                nearbyList.Add(new NearbyIssueResponse
                {
                    Id = summary.Id,
                    PublicCode = summary.PublicCode,
                    Title = summary.Title,
                    IssueType = summary.IssueType,
                    Area = summary.Area,
                    Priority = summary.Priority,
                    Status = summary.Status,
                    Latitude = summary.Latitude,
                    Longitude = summary.Longitude,
                    ThumbnailUrl = summary.ThumbnailUrl,
                    UpvoteCount = summary.UpvoteCount,
                    HasUpvoted = summary.HasUpvoted,
                    ReportedAt = summary.ReportedAt,
                    DistanceMeters = Math.Round(distance, 1)
                });
            }
        }

        var result = nearbyList
            .OrderBy(n => n.DistanceMeters)
            .Take(request.Limit)
            .ToList();

        return new ApiResponse<IReadOnlyList<NearbyIssueResponse>>
        {
            Success = true,
            Data = result
        };
    }

    public async Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetIssueTimelineAsync(
        long issueId,
        CancellationToken cancellationToken = default)
    {
        var issueExists = await _context.Issues.AnyAsync(i => i.IssueId == issueId, cancellationToken);
        if (!issueExists)
        {
            return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>
            {
                Success = false,
                Message = $"Không tìm thấy báo cáo sự cố có ID = {issueId}"
            };
        }

        var updates = await _context.IssueUpdates
            .Include(u => u.FromStatus)
            .Include(u => u.ToStatus)
            .Include(u => u.Attachments)
            .Where(u => u.IssueId == issueId)
            .OrderBy(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var timelineItems = updates.Select(u => new IssueTimelineItemResponse
        {
            Id = u.Id,
            UpdateType = u.Note != null && u.Note.StartsWith("Đã định tuyến")
                ? "ROUTED"
                : u.Note != null && u.Note.StartsWith("Đã chuyển đơn vị")
                    ? "REASSIGNED"
                    : u.FromStatusId == null ? "CREATED" : "STATUS_CHANGE",
            FromStatus = u.FromStatus != null ? new LookupItemResponse
            {
                Id = u.FromStatus.StatusId,
                Name = u.FromStatus.StatusName,
                Code = u.FromStatus.StatusCode
            } : null,
            ToStatus = u.ToStatus != null ? new LookupItemResponse
            {
                Id = u.ToStatus.StatusId,
                Name = u.ToStatus.StatusName,
                Code = u.ToStatus.StatusCode
            } : null,
            ProgressPercent = u.ProgressPercent,
            Note = u.Note,
            IsSystemGenerated = u.IsSystemGenerated,
            CreatedAt = u.CreatedAt,
            Attachments = u.Attachments.Select(MapAttachment).ToList()
        }).ToList();

        return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>
        {
            Success = true,
            Data = timelineItems
        };
    }

    private static IssueSummaryResponse MapToSummaryResponse(Issue issue, bool hasUpvoted)
    {
        var firstAttachment = issue.Attachments.FirstOrDefault();
        return new IssueSummaryResponse
        {
            Id = issue.IssueId,
            PublicCode = issue.PublicCode,
            Title = issue.Title,
            IssueType = new LookupItemResponse
            {
                Id = issue.IssueType.IssueTypeId,
                Name = issue.IssueType.TypeName,
                Code = issue.IssueType.TypeCode
            },
            Area = new LookupItemResponse
            {
                Id = issue.Area.AreaId,
                Name = issue.Area.AreaName,
                Code = issue.Area.AreaCode
            },
            Priority = new LookupItemResponse
            {
                Id = issue.Priority.PriorityId,
                Name = issue.Priority.PriorityName,
                Code = issue.Priority.PriorityCode
            },
            Status = new LookupItemResponse
            {
                Id = issue.Status.StatusId,
                Name = issue.Status.StatusName,
                Code = issue.Status.StatusCode
            },
            Latitude = issue.Latitude,
            Longitude = issue.Longitude,
            ThumbnailUrl = firstAttachment?.ThumbnailUrl ?? firstAttachment?.FileUrl,
            UpvoteCount = issue.UpvoteCount,
            HasUpvoted = hasUpvoted,
            ReportedAt = issue.ReportedAt
        };
    }

    private static IssueDetailResponse MapToDetailResponse(Issue issue, string reporterDisplayName, bool hasUpvoted)
    {
        var summary = MapToSummaryResponse(issue, hasUpvoted);
        return new IssueDetailResponse
        {
            Id = summary.Id,
            PublicCode = summary.PublicCode,
            Title = summary.Title,
            IssueType = summary.IssueType,
            Area = summary.Area,
            Priority = summary.Priority,
            Status = summary.Status,
            Latitude = summary.Latitude,
            Longitude = summary.Longitude,
            ThumbnailUrl = summary.ThumbnailUrl,
            UpvoteCount = summary.UpvoteCount,
            HasUpvoted = summary.HasUpvoted,
            ReportedAt = summary.ReportedAt,
            Description = issue.Description,
            AddressText = issue.AddressText,
            ReporterDisplayName = reporterDisplayName,
            ResolvedAt = issue.ResolvedAt,
            ClosedAt = issue.ClosedAt,
            UpdatedAt = issue.ReportedAt,
            Sla = issue.Sla != null ? new IssueSlaResponse
            {
                Id = issue.Sla.Id,
                IssueId = issue.Sla.IssueId,
                SlaPolicyId = issue.Sla.SlaPolicyId,
                FirstResponseMinutes = issue.Sla.FirstResponseMinutes,
                ResolutionMinutes = issue.Sla.ResolutionMinutes,
                FirstResponseDueAt = issue.Sla.FirstResponseDueAt,
                ResolutionDueAt = issue.Sla.ResolutionDueAt,
                FirstRespondedAt = issue.Sla.FirstRespondedAt,
                ResolvedAt = issue.Sla.ResolvedAt,
                IsFirstResponseBreached = issue.Sla.IsFirstResponseBreached,
                IsResolutionBreached = issue.Sla.IsResolutionBreached
            } : null,
            Attachments = issue.Attachments.Select(MapAttachment).ToList()
        };
    }

    private static IssueAttachmentResponse MapAttachment(IssueAttachment a)
    {
        return new IssueAttachmentResponse
        {
            Id = a.Id,
            Kind = a.Kind,
            FileUrl = a.FileUrl,
            ThumbnailUrl = a.ThumbnailUrl,
            MimeType = a.MimeType,
            FileSizeBytes = a.FileSizeBytes,
            WidthPx = a.WidthPx,
            HeightPx = a.HeightPx,
            CreatedAt = a.CreatedAt
        };
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Trái Đất bán kính tính bằng mét
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle) => (Math.PI / 180) * angle;
}
