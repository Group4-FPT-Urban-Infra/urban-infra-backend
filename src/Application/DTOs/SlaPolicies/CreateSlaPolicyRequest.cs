using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.SlaPolicies;

public class CreateSlaPolicyRequest
{
    [Required]
    public int IssueTypeId { get; set; }

    [Required]
    public byte PriorityId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ResolutionMinutes { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int FirstResponseMinutes { get; set; }
}
