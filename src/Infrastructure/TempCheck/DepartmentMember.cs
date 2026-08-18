using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class DepartmentMember
{
    public int DepartmentId { get; set; }

    public string UserId { get; set; } = null!;

    public string? JobTitle { get; set; }

    public bool IsManager { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
