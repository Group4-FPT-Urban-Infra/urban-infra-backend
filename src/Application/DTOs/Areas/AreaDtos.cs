using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace UrbanInfraSystem.Application.DTOs.Areas;

public class CreateAreaRequest
{
    public int? ParentAreaId { get; set; }

    [Required(ErrorMessage = "Mã khu vực không được để trống")]
    [MaxLength(30, ErrorMessage = "Mã khu vực tối đa 30 ký tự")]
    public string AreaCode { get; set; } = default!;

    [Required(ErrorMessage = "Tên khu vực không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên khu vực tối đa 150 ký tự")]
    public string AreaName { get; set; } = default!;

    [Required(ErrorMessage = "Loại khu vực không được để trống")]
    [MaxLength(30, ErrorMessage = "Loại khu vực tối đa 30 ký tự")]
    public string AreaType { get; set; } = default!;

    /// <summary>
    /// Ranh giới khu vực dạng GeoJSON (Polygon / MultiPolygon) truyền dưới dạng JSON object hoặc string GeoJSON.
    /// </summary>
    public JsonNode? Boundary { get; set; }

    public decimal? CentroidLatitude { get; set; }

    public decimal? CentroidLongitude { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateAreaRequest
{
    public int? ParentAreaId { get; set; }

    [Required(ErrorMessage = "Mã khu vực không được để trống")]
    [MaxLength(30, ErrorMessage = "Mã khu vực tối đa 30 ký tự")]
    public string AreaCode { get; set; } = default!;

    [Required(ErrorMessage = "Tên khu vực không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên khu vực tối đa 150 ký tự")]
    public string AreaName { get; set; } = default!;

    [Required(ErrorMessage = "Loại khu vực không được để trống")]
    [MaxLength(30, ErrorMessage = "Loại khu vực tối đa 30 ký tự")]
    public string AreaType { get; set; } = default!;

    public JsonNode? Boundary { get; set; }

    public decimal? CentroidLatitude { get; set; }

    public decimal? CentroidLongitude { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AreaResponse
{
    public int AreaId { get; set; }

    public int? ParentAreaId { get; set; }

    public string? ParentAreaName { get; set; }

    public string AreaCode { get; set; } = default!;

    public string AreaName { get; set; } = default!;

    public string AreaType { get; set; } = default!;

    public JsonNode? Boundary { get; set; }

    public decimal? CentroidLatitude { get; set; }

    public decimal? CentroidLongitude { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<AreaResponse>? SubAreas { get; set; }
}

public class AreaFilterRequest
{
    public int? ParentAreaId { get; set; }

    public string? AreaType { get; set; }

    public bool? IsActive { get; set; }

    public string? Search { get; set; }
}
