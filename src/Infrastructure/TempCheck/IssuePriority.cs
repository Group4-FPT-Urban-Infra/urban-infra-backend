using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class IssuePriority
{
    public int PriorityId { get; set; }

    public string PriorityCode { get; set; } = null!;

    public string PriorityName { get; set; } = null!;

    public byte SeverityRank { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();

    public virtual ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
}
