namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Wrapper chứa kết quả phân trang cho các endpoint trả về danh sách.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của từng phần tử trong danh sách.</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    /// <summary>Tổng số bản ghi khớp với bộ lọc (không tính phân trang).</summary>
    public int TotalCount { get; set; }

    /// <summary>Số trang hiện tại (1-indexed).</summary>
    public int Page { get; set; }

    /// <summary>Số phần tử tối đa mỗi trang.</summary>
    public int PageSize { get; set; }

    /// <summary>Tổng số trang.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Có trang kế tiếp không.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Có trang trước đó không.</summary>
    public bool HasPreviousPage => Page > 1;
}
