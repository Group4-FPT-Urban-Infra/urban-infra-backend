using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.SlaPolicies;

public class SearchSlaPoliciesRequest
{
    [MaxLength(100)]
    public string? Keyword { get; set; }

    public int? PriorityId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
