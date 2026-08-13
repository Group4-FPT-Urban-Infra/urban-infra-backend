using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;

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

    [HttpGet("stats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardStatsResponse>>> GetStats(
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetDashboardStatsAsync(cancellationToken);
        return Ok(result);
    }
}
