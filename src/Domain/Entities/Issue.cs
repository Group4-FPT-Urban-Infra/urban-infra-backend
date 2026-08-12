using System;
using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Minimal Issue entity to support nearby duplicate detection.
/// Includes location (Latitude/Longitude) and lightweight metadata used by responses.
/// </summary>
public class Issue
{
    public long IssueId { get; set; }

    public string PublicCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int IssueTypeId { get; set; }

    public int? AreaId { get; set; }

    public int? PriorityId { get; set; }

    public int? StatusId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int UpvoteCount { get; set; }

    public DateTime ReportedAt { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool IsArchived { get; set; } = false;

    // Navigation
    public IssueType? IssueType { get; set; }
    public IssuePriority? Priority { get; set; }
    public IssueStatus? Status { get; set; }
}
