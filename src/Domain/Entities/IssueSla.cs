using System;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Minimal IssueSla entity representing SLA instance for an Issue.
/// Used by the SLA breach detection job.
/// </summary>
public class IssueSla
{
    public long IssueSlaId { get; set; }

    public long IssueId { get; set; }

    public Guid SlaPolicyId { get; set; }

    /// <summary>
    /// Resolution due datetime (UTC).
    /// </summary>
    public DateTime ResolutionDueAtUtc { get; set; }

    /// <summary>
    /// Whether SLA is completed/resolved (do not escalate completed SLAs).
    /// </summary>
    public bool IsCompleted { get; set; }

    public Issue? Issue { get; set; }
}
