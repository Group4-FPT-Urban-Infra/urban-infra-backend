using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.API.Contracts.Issues;

/// <summary>Dữ liệu multipart/form-data dùng khi công dân tạo báo cáo.</summary>
public class CreateIssueFormRequest
{
    [Range(1, int.MaxValue)]
    public int IssueTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int AreaId { get; set; }

    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MinLength(10), MaxLength(2000)]
    public string Description { get; set; } = default!;

    [MaxLength(300)]
    public string? AddressText { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    /// <summary>Tối đa 5 ảnh JPEG, PNG hoặc WebP; mỗi ảnh tối đa 5 MB.</summary>
    public List<IFormFile> Images { get; set; } = [];
}
