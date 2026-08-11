using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.RoutingRules;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/routing-rules")]
[Tags("Routing Rules")]
[Authorize(Roles = Roles.Admin)]
public class RoutingRulesController : ControllerBase
{
    private readonly IRoutingRuleService _service;

    public RoutingRulesController(IRoutingRuleService service) => _service = service;

    /// <summary>Lấy danh sách rule, hỗ trợ lọc theo loại sự cố, khu vực và đơn vị.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoutingRuleResponse>>> GetAll(
        [FromQuery] RoutingRuleFilterRequest filter,
        CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(filter, cancellationToken));

    /// <summary>Lấy chi tiết một quy tắc định tuyến.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoutingRuleResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var rule = await _service.GetByIdAsync(id, cancellationToken);
        return rule is null ? NotFound(new { message = $"Không tìm thấy quy tắc có ID = {id}." }) : Ok(rule);
    }

    /// <summary>Tìm đơn vị xử lý theo đúng cặp issueTypeId và areaId.</summary>
    [HttpGet("resolve")]
    public async Task<ActionResult<RoutingRuleResponse>> Resolve(
        [FromQuery] int issueTypeId,
        [FromQuery] int areaId,
        CancellationToken cancellationToken)
    {
        if (issueTypeId <= 0 || areaId <= 0)
            return BadRequest(new { message = "issueTypeId và areaId phải lớn hơn 0." });
        var rule = await _service.ResolveAsync(issueTypeId, areaId, cancellationToken);
        return rule is null
            ? NotFound(new { message = "Không tìm thấy quy tắc định tuyến đang hoạt động cho loại sự cố và khu vực này." })
            : Ok(rule);
    }

    /// <summary>Tạo quy tắc issue type + area → department.</summary>
    [HttpPost]
    public async Task<ActionResult<RoutingRuleResponse>> Create(
        [FromBody] CreateRoutingRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = rule.RoutingRuleId }, rule);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Cập nhật quy tắc định tuyến.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoutingRuleResponse>> Update(
        int id,
        [FromBody] UpdateRoutingRuleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rule = await _service.UpdateAsync(id, request, cancellationToken);
            return rule is null ? NotFound(new { message = $"Không tìm thấy quy tắc có ID = {id}." }) : Ok(rule);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Vô hiệu hóa quy tắc để giữ lịch sử định tuyến.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await _service.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new { message = $"Không tìm thấy quy tắc có ID = {id}." });
}
