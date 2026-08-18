using UrbanInfraSystem.Application.DTOs.Staff;
using System.Collections.Generic;
using UrbanInfraSystem.Application.DTOs.Issues;

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
    Task<List<StaffMapIssueResponse>> GetMapIssuesAsync(string staffUserId, StaffIncidentFilterRequest filters, CancellationToken cancellationToken);

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
}
