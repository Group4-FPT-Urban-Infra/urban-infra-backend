namespace UrbanInfraSystem.Application.DTOs.IssueTypes;

/// <summary>
/// Response trả về chi tiết một loại sự cố, bao gồm cả thông tin cha và danh sách con.
/// </summary>
public class IssueTypeResponse
{
    public int IssueTypeId { get; set; }
    public int? ParentIssueTypeId { get; set; }
    public string TypeCode { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Thông tin loại sự cố cha (nếu có).</summary>
    public IssueTypeSummary? ParentIssueType { get; set; }

    /// <summary>Danh sách các loại sự cố con trực tiếp.</summary>
    public IReadOnlyList<IssueTypeSummary> SubIssueTypes { get; set; } = [];
}

/// <summary>
/// Response rút gọn cho lookup/dropdown, chỉ gồm thông tin cơ bản.
/// </summary>
public class IssueTypeLookupResponse
{
    public int IssueTypeId { get; set; }
    public int? ParentIssueTypeId { get; set; }
    public string TypeCode { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Response cho lookup dạng cây (nested), dùng khi mode=tree.
/// </summary>
public class IssueTypeTreeResponse
{
    public int IssueTypeId { get; set; }
    public string TypeCode { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }

    /// <summary>Danh sách các loại sự cố con trực tiếp.</summary>
    public List<IssueTypeTreeResponse> Children { get; set; } = [];
}

/// <summary>
/// Summary dùng trong nested response (ParentIssueType và SubIssueTypes).
/// </summary>
public class IssueTypeSummary
{
    public int IssueTypeId { get; set; }
    public string TypeCode { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
}
