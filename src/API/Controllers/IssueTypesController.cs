using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.IssueTypes;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// API quản lý danh mục loại sự cố hạ tầng đô thị.
/// Chỉ Admin mới có quyền tạo/sửa/xóa. GET lookup công khai cho Citizen tạo báo cáo.
/// </summary>
[ApiController]
[Route("api/issue-types")]
[Produces("application/json")]
[Tags("IssueTypes")]
public class IssueTypesController : ControllerBase
{
    private readonly IIssueTypeService _issueTypeService;

    public IssueTypesController(IIssueTypeService issueTypeService)
    {
        _issueTypeService = issueTypeService;
    }

    /// <summary>Tạo mới một loại sự cố.</summary>
    /// <remarks>
    /// Ví dụ:
    /// ```
    /// POST /api/issue-types
    /// {
    ///     "typeCode": "LIGHT",
    ///     "typeName": "Đèn đường",
    ///     "parentIssueTypeId": null,
    ///     "iconUrl": "https://example.com/icons/light.png",
    ///     "isActive": true
    /// }
    /// ```
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateIssueTypeRequest request)
    {
        var result = await _issueTypeService.CreateAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.IssueTypeId }, result);
    }

    /// <summary>Lấy danh sách loại sự cố với phân trang và bộ lọc.</summary>
    /// <remarks>
    /// Ví dụ:
    /// ```
    /// GET /api/issue-types?page=1&amp;pageSize=20&amp;keyword=đèn&amp;isActiveOnly=true
    /// ```
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchIssueTypesRequest request)
    {
        var result = await _issueTypeService.SearchAsync(request);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết một loại sự cố theo ID.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _issueTypeService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>Cập nhật thông tin loại sự cố.</summary>
    /// <remarks>
    /// Ví dụ:
    /// ```
    /// PUT /api/issue-types/1
    /// {
    ///     "typeName": "Đèn đường LED",
    ///     "isActive": true
    /// }
    /// ```
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IssueTypeResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIssueTypeRequest request)
    {
        var result = await _issueTypeService.UpdateAsync(id, request);

        if (!result.Success)
        {
            if (result.Message?.Contains("không tồn tại") == true)
            {
                return NotFound(result);
            }

            if (result.Message?.Contains("đã tồn tại") == true || result.Message?.Contains("vòng lặp") == true)
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>Xóa (soft delete) một loại sự cố.</summary>
    /// <remarks>
    /// Xóa mềm: đánh dấu IsDeleted = true và IsActive = false.
    /// Không thể xóa nếu còn loại con đang hoạt động.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _issueTypeService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Message?.Contains("không tồn tại") == true)
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>Lấy danh sách loại sự cố cho dropdown/lookup (chỉ các loại đang hoạt động).</summary>
    /// <remarks>
    /// Endpoint công khai, không yêu cầu đăng nhập.
    /// Dùng cho Citizen chọn loại sự cố khi tạo báo cáo trên bản đồ.
    /// </remarks>
    /// <param name="mode">"flat" - danh sách phẳng (mặc định), "tree" - cây phân cấp</param>
    [HttpGet("lookup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLookup([FromQuery] string mode = "flat")
    {
        var result = await _issueTypeService.GetLookupAsync(mode);
        return Ok(result);
    }
}
