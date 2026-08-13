using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.IssueTypes;

/// <summary>
/// Request tạo mới một loại sự cố. Chỉ Admin mới có quyền thực hiện.
/// </summary>
public class CreateIssueTypeRequest
{
    /// <summary>Mã loại sự cố, unique, viết hoa không dấu (VD: "LIGHT", "POTHOLE").</summary>
    [Required, MaxLength(30)]
    public string TypeCode { get; set; } = default!;

    /// <summary>Tên hiển thị của loại sự cố.</summary>
    [Required, MaxLength(150)]
    public string TypeName { get; set; } = default!;

    /// <summary>Mã loại sự cố cha (nullable). Nếu là loại cấp cao nhất thì bỏ trống.</summary>
    public int? ParentIssueTypeId { get; set; }

    /// <summary>URL icon mô tả loại sự cố (dùng cho bản đồ/marker).</summary>
    [MaxLength(1000)]
    public string? IconUrl { get; set; }

    /// <summary>Mô tả chi tiết loại sự cố.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Trạng thái hoạt động. Mặc định true.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request cập nhật thông tin loại sự cố.
/// </summary>
public class UpdateIssueTypeRequest
{
    /// <summary>Mã loại sự cố mới (nullable - chỉ cập nhật nếu được truyền).</summary>
    [MaxLength(30)]
    public string? TypeCode { get; set; }

    /// <summary>Tên hiển thị mới.</summary>
    [MaxLength(150)]
    public string? TypeName { get; set; }

    /// <summary>Mã loại sự cố cha mới (nullable).</summary>
    public int? ParentIssueTypeId { get; set; }

    /// <summary>URL icon mới.</summary>
    [MaxLength(1000)]
    public string? IconUrl { get; set; }

    /// <summary>Mô tả chi tiết mới.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Trạng thái hoạt động.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request tìm kiếm/phân trang danh sách loại sự cố.
/// </summary>
public class SearchIssueTypesRequest
{
    /// <summary>Lọc theo mã loại cha trực tiếp.</summary>
    public int? ParentIssueTypeId { get; set; }

    /// <summary>Lọc theo từ khóa (tìm trong typeCode và typeName).</summary>
    [MaxLength(100)]
    public string? Keyword { get; set; }

    /// <summary>Chỉ lấy các loại cấp cao nhất (không có cha).</summary>
    public bool? IsRootOnly { get; set; }

    /// <summary>Chỉ lấy các loại cấp con (có cha).</summary>
    public bool? IsSubCategoryOnly { get; set; }

    /// <summary>Chỉ lấy các loại đang hoạt động.</summary>
    public bool? IsActiveOnly { get; set; }

    /// <summary>Trang hiện tại (1-based).</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Số bản ghi mỗi trang.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>Cột sắp xếp. VD: "typeCode", "typeName", "createdAtDesc".</summary>
    public string Sort { get; set; } = "typeName";
}
