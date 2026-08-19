using UrbanInfraSystem.Application.DTOs.Staff;
using System.Collections.Generic;
using UrbanInfraSystem.Application.DTOs.Issues;
using Microsoft.AspNetCore.Http;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Giao diện cho các nghiệp vụ dành riêng cho Cán bộ xử lý (DepartmentStaff).
/// </summary>
public interface IStaffService
{
    /// <summary>
    /// Lấy dữ liệu tóm tắt cho dashboard của cán bộ xử lý đang đăng nhập.
    /// Dữ liệu được tổng hợp dựa trên đơn vị mà cán bộ đó thuộc về.
    /// </summary>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dữ liệu tóm tắt cho dashboard.</returns>
    Task<StaffDashboardSummaryResponse> GetDashboardSummaryAsync(string staffUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy danh sách các công việc (sự cố) đang mở được giao cho đơn vị của cán bộ.
    /// </summary>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Danh sách các công việc cần xử lý.</returns>
    Task<List<StaffTaskResponse>> GetMyTasksAsync(string staffUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy danh sách các hoạt động gần đây trên các sự cố được giao cho đơn vị của cán bộ.
    /// </summary>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Danh sách các hoạt động gần đây.</returns>
    Task<List<StaffActivityResponse>> GetMyRecentActivitiesAsync(string staffUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy danh sách các sự cố có phân trang và lọc, trong phạm vi của cán bộ.
    /// </summary>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="filters">Các tham số lọc và phân trang.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Kết quả phân trang của các sự cố.</returns>
    Task<PagedResponse<StaffIncidentResponse>> GetIncidentsAsync(string staffUserId, StaffIncidentFilterRequest filters, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy danh sách các sự cố để hiển thị trên bản đồ của cán bộ, có áp dụng bộ lọc.
    /// </summary>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="filters">Các tham số lọc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Danh sách các sự cố phù hợp để hiển thị trên bản đồ.</returns>
    Task<List<StaffMapIssueResponse>> GetMapIssuesAsync(string staffUserId, StaffMapFilterRequest filters, CancellationToken cancellationToken);

    /// <summary>
    /// Cho phép cán bộ đang đăng nhập nhận (claim) một sự cố chưa được phân công.
    /// Thao tác này sẽ gán sự cố cho đơn vị của cán bộ đó.
    /// </summary>
    /// <param name="issueId">ID của sự cố cần nhận.</param>
    /// <param name="staffUserId">ID của cán bộ thực hiện.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="KeyNotFoundException">Khi không tìm thấy sự cố.</exception>
    /// <exception cref="InvalidOperationException">Khi sự cố đã được phân công.</exception>
    /// <exception cref="UnauthorizedAccessException">Khi người dùng không thuộc đơn vị nào.</exception>
    Task ClaimIncidentAsync(long issueId, string staffUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy chi tiết một sự cố cụ thể để hiển thị cho cán bộ xử lý.
    /// </summary>
    /// <param name="issueId">ID của sự cố cần lấy chi tiết.</param>
    /// <param name="staffUserId">ID của cán bộ đang đăng nhập.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chi tiết sự cố hoặc null nếu không có quyền truy cập.</returns>
    Task<StaffIncidentDetailResponse?> GetIncidentDetailAsync(long issueId, string staffUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Cán bộ cập nhật trạng thái sự cố và/hoặc tải lên bằng chứng xử lý.
    /// Tạo một IssueUpdate mới với các file đính kèm nếu có.
    /// </summary>
    /// <param name="issueId">ID của sự cố.</param>
    /// <param name="request">Yêu cầu cập nhật kèm danh sách file ảnh.</param>
    /// <param name="staffUserId">ID của cán bộ thực hiện.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="KeyNotFoundException">Khi không tìm thấy sự cố.</exception>
    /// <exception cref="UnauthorizedAccessException">Khi không có quyền.</exception>
    Task<StaffIncidentDetailResponse> UpdateIssueAsync(long issueId, StaffUpdateIssueRequest request, string staffUserId, CancellationToken cancellationToken);
}
