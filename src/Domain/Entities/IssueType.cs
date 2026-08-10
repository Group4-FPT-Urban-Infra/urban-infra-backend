using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity cho danh mục loại sự cố hạ tầng đô thị (VD: Đèn đường, Ổ gà, Rác thải...).
/// Hỗ trợ phân cấp cha-con thông qua parent_issue_type_id.
/// </summary>
public class IssueType : BaseEntity
{
    /// <summary>Mã loại sự cố, dùng làm mã định danh ngắn gọn (VD: "LIGHT", "POTHOLE").</summary>
    public int IssueTypeId { get; set; }

    /// <summary>Mã loại sự cố cha (nullable). VD: "Đèn đường" có thể có con "Đèn LED", "Đèn cao áp".</summary>
    public int? ParentIssueTypeId { get; set; }

    /// <summary>Mã loại sự cố, unique, dùng trong code logic.</summary>
    public string TypeCode { get; set; } = default!;

    /// <summary>Tên hiển thị của loại sự cố.</summary>
    public string TypeName { get; set; } = default!;

    /// <summary>URL icon mô tả loại sự cố (dùng cho bản đồ/tr marker).</summary>
    public string? IconUrl { get; set; }

    /// <summary>Mô tả chi tiết loại sự cố.</summary>
    public string? Description { get; set; }

    /// <summary>Đánh dấu loại sự cố có đang hoạt động hay không.</summary>
    public bool IsActive { get; set; } = true;

    // ==================== Navigation Properties ====================

    /// <summary>Loại sự cố cha (self-referencing FK).</summary>
    public IssueType? ParentIssueType { get; set; }

    /// <summary>Danh sách loại sự cố con.</summary>
    public ICollection<IssueType> SubIssueTypes { get; set; } = new List<IssueType>();
}
