using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.IssuePriorities;

public class CreateIssuePriorityRequest
{
    [Required, MaxLength(20)]
    public string PriorityCode { get; set; } = default!;

    [Required, MaxLength(50)]
    public string PriorityName { get; set; } = default!;

    [Range(1, byte.MaxValue)]
    public byte SeverityRank { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateIssuePriorityRequest
{
    [MaxLength(20)]
    public string? PriorityCode { get; set; }

    [MaxLength(50)]
    public string? PriorityName { get; set; }

    [Range(1, byte.MaxValue)]
    public byte? SeverityRank { get; set; }

    public bool? IsActive { get; set; }
}

public class IssuePriorityResponse
{
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = default!;
    public string PriorityName { get; set; } = default!;
    public byte SeverityRank { get; set; }
    public bool IsActive { get; set; }
}
