using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IIssueService _issueService;

    public DashboardController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    /// <summary>Thống kê tổng quan công khai (dùng cho citizen/public).</summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardStatsResponse>>> GetStats(
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetDashboardStatsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 6 chỉ số KPI dành riêng cho Admin Dashboard, kèm xu hướng % so với tuần trước.
    /// </summary>
    [HttpGet("admin-kpis")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AdminKpiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AdminKpiResponse>>> GetAdminKpis(
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetAdminKpiAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Dữ liệu biểu đồ xu hướng sự cố theo period.
    /// period: "ThisWeek" (7 ngày) | "ThisMonth" (từng ngày) | "ThisYear" (12 tháng).
    /// Mặc định: ThisWeek.
    /// </summary>
    [HttpGet("incident-trends")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrendDataPoint>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrendDataPoint>>>> GetIncidentTrends(
        [FromQuery] string period = "ThisWeek",
        CancellationToken cancellationToken = default)
    {
        var result = await _issueService.GetIncidentTrendsAsync(period, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Phân bổ sự cố theo danh mục (tỷ lệ phần trăm).
    /// </summary>
    [HttpGet("category-distribution")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDistributionPoint>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDistributionPoint>>>> GetCategoryDistribution(
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetCategoryDistributionAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Dữ liệu heatmap các cụm sự cố theo khu vực trong khoảng thời gian.
    /// timeframe: "24h" | "7d" | "30d"
    /// </summary>
    [HttpGet("heatmap")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HeatmapDataPoint>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HeatmapDataPoint>>>> GetIncidentHeatmap(
        [FromQuery] string timeframe = "7d",
        CancellationToken cancellationToken = default)
    {
        var result = await _issueService.GetIncidentHeatmapAsync(timeframe, cancellationToken);
        return Ok(result);
    }
}

