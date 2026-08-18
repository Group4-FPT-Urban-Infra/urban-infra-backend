using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.DepartmentManager;

public class DepartmentManagerIssueListRequest
{
    public string? Filter { get; set; }
    public List<int>? StatusIds { get; set; }
    public List<int>? PriorityIds { get; set; }
    public List<int>? IssueTypeIds { get; set; }
    public int? AreaId { get; set; }
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AssignIssueRequest
{
    [Required]
    public string UserId { get; set; } = default!;

    public string? Note { get; set; }
}

public class DepartmentManagerUpdateIssueStatusRequest
{
    [Required]
    public int StatusId { get; set; }

    public string? Note { get; set; }
    public byte? ProgressPercent { get; set; }
}

public class DepartmentManagerIssueDetailResponse
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string IssueTypeName { get; set; } = default!;
    public string PriorityName { get; set; } = default!;
    public string StatusName { get; set; } = default!;
    public string AddressText { get; set; } = default!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsPublic { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public CurrentAssignmentInfo? CurrentAssignment { get; set; }
    public List<AssignmentMemberInfo> AssignedMembers { get; set; } = new();
    public List<IssueUpdateInfo> Updates { get; set; } = new();
}

public class CurrentAssignmentInfo
{
    public long AssignmentId { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = default!;
    public DateTime AssignedAt { get; set; }
}

public class AssignmentMemberInfo
{
    public long MemberId { get; set; }
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = default!;
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Note { get; set; }
}

public class IssueUpdateInfo
{
    public long Id { get; set; }
    public string CreatedByName { get; set; } = default!;
    public string? FromStatusName { get; set; }
    public string ToStatusName { get; set; } = default!;
    public string? Note { get; set; }
    public byte? ProgressPercent { get; set; }
    public bool IsSystemGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
