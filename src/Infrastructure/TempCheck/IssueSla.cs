using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class IssueSla
{
    public long IssueSlaId { get; set; }

    public long IssueId { get; set; }

    public Guid SlaPolicyId { get; set; }

    public DateTime ResolutionDueAtUtc { get; set; }

    public bool IsCompleted { get; set; }

    public virtual Issue Issue { get; set; } = null!;
}
