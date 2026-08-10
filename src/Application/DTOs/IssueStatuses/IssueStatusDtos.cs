using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.IssueStatuses;

public class CreateIssueStatusRequest
{
    [Required, MaxLength(30)]
    public string StatusCode { get; set; } = default!;

    [Required, MaxLength(80)]
    public string StatusName { get; set; } = default!;

    public bool IsClosed { get; set; }
    public bool IsPublicVisible { get; set; } = true;

    [Range(1, short.MaxValue)]
    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateIssueStatusRequest
{
    [MaxLength(30)]
    public string? StatusCode { get; set; }

    [MaxLength(80)]
    public string? StatusName { get; set; }

    public bool? IsClosed { get; set; }
    public bool? IsPublicVisible { get; set; }

    [Range(1, short.MaxValue)]
    public short? DisplayOrder { get; set; }

    public bool? IsActive { get; set; }
}

public class IssueStatusResponse
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = default!;
    public string StatusName { get; set; } = default!;
    public bool IsClosed { get; set; }
    public bool IsPublicVisible { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
