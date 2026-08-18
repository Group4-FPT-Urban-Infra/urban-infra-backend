using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class IssueStatus
{
    public int StatusId { get; set; }

    public string StatusCode { get; set; } = null!;

    public string StatusName { get; set; } = null!;

    public bool IsClosed { get; set; }

    public bool IsPublicVisible { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
