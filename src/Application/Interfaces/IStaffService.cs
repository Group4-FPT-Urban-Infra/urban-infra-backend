using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Staff;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IStaffService
{
    /// <summary>
    /// Lấy các số liệu tổng quan cho dashboard của nhân viên.
    /// </summary>
    Task<StaffDashboardSummaryResponse?> GetDashboardSummaryAsync(
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các công việc (đã chấp nhận) của nhân viên hiện tại có phân trang.
    /// </summary>
    Task<PagedResponse<StaffTaskResponse>> GetMyTasksAsync(
        GetMyTasksRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các phân công đang chờ nhân viên phản hồi có phân trang.
    /// </summary>
    Task<PagedResponse<StaffAssignmentResponse>> GetMyPendingAssignmentsAsync(
        GetMyAssignmentsRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Nhân viên chấp nhận hoặc từ chối một phân công.
    /// </summary>
    Task RespondToAssignmentAsync(
        long assignmentId, 
        RespondToAssignmentRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy các hoạt động gần đây trong phòng ban của nhân viên có phân trang.
    /// </summary>
    Task<PagedResponse<StaffActivityResponse>?> GetMyRecentActivitiesAsync(
        PagedRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm và lấy danh sách các sự cố thuộc phòng ban của nhân viên có phân trang.
    /// </summary>
    Task<PagedResponse<StaffIncidentResponse>?> GetIncidentsAsync(
        SearchStaffIncidentsRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chi tiết của một sự cố.
    /// </summary>
    Task<StaffIncidentDetailResponse?> GetIncidentDetailAsync(
        long issueId, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các sự cố trong một khung nhìn bản đồ.
    /// </summary>
    Task<IReadOnlyList<StaffMapIssueResponse>?> GetMapIssuesAsync(
        GetMapIssuesRequest request, 
        string staffId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Nhân viên tự nhận (claim) một sự cố chưa được gán.
    /// </summary>
    Task ClaimIncidentAsync(
        long issueId, 
        string staffId, 
        CancellationToken cancellationToken = default);
}