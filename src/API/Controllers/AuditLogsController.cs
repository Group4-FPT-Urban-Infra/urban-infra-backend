using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Tags("Audit Logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IIssueService _issueService;

    public AuditLogsController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    /// <summary>
    /// Danh sách các sự kiện/hoạt động hệ thống mới nhất.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditLogResponse>>>> GetAuditLogs(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _issueService.GetRecentAuditLogsAsync(limit, cancellationToken);
        return Ok(result);
    }
}
