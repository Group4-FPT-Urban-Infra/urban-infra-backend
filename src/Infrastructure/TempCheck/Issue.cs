using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class Issue
{
    public long IssueId { get; set; }

    public string PublicCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int IssueTypeId { get; set; }

    public int? AreaId { get; set; }

    public int? PriorityId { get; set; }

    public int? StatusId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int UpvoteCount { get; set; }

    public DateTime ReportedAt { get; set; }

    public bool IsPublic { get; set; }

    public bool IsArchived { get; set; }

    public virtual ICollection<EscalationEvent> EscalationEvents { get; set; } = new List<EscalationEvent>();

    public virtual ICollection<IssueSla> IssueSlas { get; set; } = new List<IssueSla>();

    public virtual IssueType IssueType { get; set; } = null!;

    public virtual IssuePriority? Priority { get; set; }

    public virtual IssueStatus? Status { get; set; }
}
