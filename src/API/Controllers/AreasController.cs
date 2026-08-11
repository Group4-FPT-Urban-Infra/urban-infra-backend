using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Areas;
using UrbanInfraSystem.Application.Interfaces;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreasController : ControllerBase
{
    private readonly IAreaService _areaService;

    public AreasController(IAreaService areaService)
    {
        _areaService = areaService;
    }

    /// <summary>
    /// Lấy danh sách các khu vực (có hỗ trợ lọc theo parent_area_id, area_type, is_active, search).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AreaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AreaResponse>>> GetAreas([FromQuery] AreaFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _areaService.GetAreasAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một khu vực theo ID (bao gồm danh sách khu vực con nếu có).
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaResponse>> GetAreaById(int id, CancellationToken cancellationToken)
    {
        var result = await _areaService.GetAreaByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy khu vực có ID = {id}" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một khu vực (hỗ trợ parent_area_id và ranh giới GeoJSON/Geography).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AreaResponse>> CreateArea([FromBody] CreateAreaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _areaService.CreateAreaAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAreaById), new { id = result.AreaId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin khu vực theo ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaResponse>> UpdateArea(int id, [FromBody] UpdateAreaRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _areaService.UpdateAreaAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa một khu vực theo ID (kiểm tra không cho xóa nếu có khu vực con phụ thuộc).
    /// </summary>
    [HttpDelete("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArea(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _areaService.DeleteAreaAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(new { message = $"Không tìm thấy khu vực có ID = {id}" });
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
