using System;
using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// SLA Policy entity mapping IssueType + Priority => response/resolve times.
/// Inherits BaseEntity to reuse auditing fields.
/// </summary>
public class SlaPolicy : BaseEntity
{
    public int IssueTypeId { get; set; }

    public int PriorityId { get; set; }

    public int ResolutionMinutes { get; set; }

    public int FirstResponseMinutes { get; set; }

    /// <summary>
    /// Số phút trước deadline mà hệ thống sẽ cảnh báo trước (WarningBefore).
    /// </summary>
    public int WarningBeforeMinutes { get; set; }

    // Navigation
    public IssueType? IssueType { get; set; }
    public IssuePriority? IssuePriority { get; set; }
}
