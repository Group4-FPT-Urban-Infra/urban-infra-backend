using System;
using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.EscalationRules;

public class CreateEscalationRuleRequest
{
    [Required]
    public Guid SlaPolicyId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int OverdueMinutes { get; set; }

    public int? TargetDepartmentId { get; set; }

    public string? TargetRoleName { get; set; }

    [Range(1, int.MaxValue)]
    public int EscalationLevel { get; set; } = 1;

    [MaxLength(250)]
    public string? NotificationTitle { get; set; }

    [MaxLength(4000)]
    public string? NotificationTemplate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateEscalationRuleRequest
{
    [Required]
    public Guid SlaPolicyId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int OverdueMinutes { get; set; }

    public int? TargetDepartmentId { get; set; }

    public string? TargetRoleName { get; set; }

    [Range(1, int.MaxValue)]
    public int EscalationLevel { get; set; } = 1;

    [MaxLength(250)]
    public string? NotificationTitle { get; set; }

    [MaxLength(4000)]
    public string? NotificationTemplate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EscalationRuleResponse
{
    public Guid Id { get; set; }
    public Guid SlaPolicyId { get; set; }
    public int OverdueMinutes { get; set; }
    public int? TargetDepartmentId { get; set; }
    public string? TargetDepartmentName { get; set; }
    public string? TargetRoleName { get; set; }
    public int EscalationLevel { get; set; }
    public string? NotificationTitle { get; set; }
    public string? NotificationTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
