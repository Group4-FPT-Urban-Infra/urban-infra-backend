using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.EscalationEvents;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/escalation-events")]
[Tags("Escalation Events")]
[Authorize]
public class EscalationEventsController : ControllerBase
{
    private readonly IEscalationEventService _service;
    private readonly ICurrentUserService _currentUser;

    public EscalationEventsController(IEscalationEventService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EscalationEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EscalationEventResponse>>> Get([FromQuery] long? issueId, [FromQuery] int? departmentId, CancellationToken ct)
    {
        if (issueId.HasValue)
        {
            var list = await _service.GetByIssueAsync(issueId.Value, ct);
            return Ok(list);
        }

        if (departmentId.HasValue)
        {
            var list = await _service.GetByDepartmentAsync(departmentId.Value, ct);
            return Ok(list);
        }

        return BadRequest(new { message = "Please specify issueId or departmentId as query parameter." });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EscalationEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EscalationEventResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(EscalationEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EscalationEventResponse>> Acknowledge([FromRoute] Guid id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return Unauthorized();

        try
        {
            var result = await _service.AcknowledgeAsync(id, _currentUser.UserId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
