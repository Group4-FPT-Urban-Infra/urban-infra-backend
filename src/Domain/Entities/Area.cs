using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Đại diện cho khu vực quản lý hạ tầng đô thị (Quận/Huyện, Phường/Xã, Khu vực...).
/// Hỗ trợ phân cấp cha-con (parent_area_id) và không gian (boundary, centroid).
/// </summary>
public class Area
{
    public int AreaId { get; set; }

    public int? ParentAreaId { get; set; }

    public string AreaCode { get; set; } = default!;

    public string AreaName { get; set; } = default!;

    public string AreaType { get; set; } = default!;

    /// <summary>
    /// Ranh giới địa lý của khu vực (GEOGRAPHY trong SQL Server).
    /// </summary>
    public Geometry? Boundary { get; set; }

    public decimal? CentroidLatitude { get; set; }

    public decimal? CentroidLongitude { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Area? ParentArea { get; set; }

    public virtual ICollection<Area> SubAreas { get; set; } = new List<Area>();
}
