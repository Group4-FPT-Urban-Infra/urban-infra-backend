using UrbanInfraSystem.Application.DTOs.IssueTypes;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Service interface cho nghiệp vụ quản lý danh mục loại sự cố.
/// </summary>
public interface IIssueTypeService
{
    /// <summary>Tạo mới một loại sự cố.</summary>
    Task<ApiResponse<IssueTypeResponse>> CreateAsync(CreateIssueTypeRequest request);

    /// <summary>Lấy chi tiết một loại sự cố theo ID.</summary>
    Task<ApiResponse<IssueTypeResponse>> GetByIdAsync(int id);

    /// <summary>Tìm kiếm và phân trang danh sách loại sự cố.</summary>
    Task<ApiResponse<PagedResponse<IssueTypeResponse>>> SearchAsync(SearchIssueTypesRequest request);

    /// <summary>Cập nhật thông tin loại sự cố.</summary>
    Task<ApiResponse<IssueTypeResponse>> UpdateAsync(int id, UpdateIssueTypeRequest request);

    /// <summary>Xóa (soft delete) một loại sự cố.</summary>
    Task<ApiResponse<bool>> DeleteAsync(int id);

    /// <summary>Lấy danh sách loại sự cố cho dropdown/lookup (chỉ các loại đang hoạt động).</summary>
    /// <param name="mode">"flat" - danh sách phẳng, "tree" - cây phân cấp (mặc định: flat)</param>
    Task<ApiResponse<object>> GetLookupAsync(string? mode = "flat");
}
