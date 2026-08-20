using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace UrbanInfraSystem.Application.DTOs.Issues;

/// <summary>Dữ liệu multipart/form-data dùng khi công dân tạo báo cáo.</summary>
public class CreateIssueFormRequest
{
    /// <summary>Giữ tương thích client cũ chỉ gửi một loại sự cố.</summary>
    [Range(1, int.MaxValue)]
    public int? IssueTypeId { get; set; }

    /// <summary>Các loại sự cố cần tách thành các Issue độc lập trong cùng một Report.</summary>
    public List<int> IssueTypeIds { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int AreaId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PriorityId { get; set; }

    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MinLength(10), MaxLength(2000)]
    public string Description { get; set; } = default!;

    /// <summary>Mô tả loại sự cố cụ thể khi chọn Issue Type có mã OTHER.</summary>
    [MaxLength(1000)]
    public string? CustomTypeDescription { get; set; }

    [MaxLength(300)]
    public string? AddressText { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    /// <summary>Tối đa 5 ảnh JPEG, PNG hoặc WebP; mỗi ảnh tối đa 5 MB.</summary>
    public List<IFormFile> Images { get; set; } = [];
}
