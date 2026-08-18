using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class Area
{
    public int AreaId { get; set; }

    public int? ParentAreaId { get; set; }

    public string AreaCode { get; set; } = null!;

    public string AreaName { get; set; } = null!;

    public string AreaType { get; set; } = null!;

    public Geometry? Boundary { get; set; }

    public decimal? CentroidLatitude { get; set; }

    public decimal? CentroidLongitude { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Area> InverseParentArea { get; set; } = new List<Area>();

    public virtual Area? ParentArea { get; set; }

    public virtual ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
}
