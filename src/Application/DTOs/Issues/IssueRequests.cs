using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.Issues;

public class SearchIssuesRequest
{
    public int? IssueTypeId { get; set; }
    public int? AreaId { get; set; }
    public string[]? StatusCodes { get; set; }
    public string[]? PriorityCodes { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [MaxLength(100)]
    public string? Keyword { get; set; }

    [Range(-90, 90)]
    public decimal? MinLatitude { get; set; }

    [Range(-90, 90)]
    public decimal? MaxLatitude { get; set; }

    [Range(-180, 180)]
    public decimal? MinLongitude { get; set; }

    [Range(-180, 180)]
    public decimal? MaxLongitude { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string Sort { get; set; } = "reportedAtDesc";
}

public class FindNearbyIssuesRequest
{
    [Range(1, int.MaxValue)]
    public int IssueTypeId { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Range(10, 2000)]
    public int RadiusMeters { get; set; } = 200;

    [Range(1, 365)]
    public int WithinDays { get; set; } = 30;

    [Range(1, 20)]
    public int Limit { get; set; } = 5;
}

public class GetMyIssuesRequest
{
    public string[]? StatusCodes { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
