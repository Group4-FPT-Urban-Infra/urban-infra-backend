using System;

namespace UrbanInfraSystem.Application.DTOs.SlaPolicies;

public class SlaPolicyResponse
{
    public Guid Id { get; set; }

    public int IssueTypeId { get; set; }

    public string IssueTypeName { get; set; } = default!;

    public int PriorityId { get; set; }

    public string PriorityName { get; set; } = default!;

    public int ResolutionMinutes { get; set; }

    public int FirstResponseMinutes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
