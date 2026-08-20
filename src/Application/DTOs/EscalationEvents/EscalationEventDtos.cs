using System;

namespace UrbanInfraSystem.Application.DTOs.EscalationEvents;

public class EscalationEventResponse
{
    public Guid Id { get; set; }
    public long IssueId { get; set; }
    public Guid? EscalationRuleId { get; set; }
    public int? TargetDepartmentId { get; set; }
    public string? TargetUserId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? EventStatus { get; set; }
    public string? Note { get; set; }
}
