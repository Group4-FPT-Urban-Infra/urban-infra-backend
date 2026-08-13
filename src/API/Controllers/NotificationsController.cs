using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Notifications;
using UrbanInfraSystem.Application.Interfaces;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// API Quản lý thông báo trong hệ thống.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lấy danh sách thông báo chưa đọc của người dùng.
    /// Nếu query parameter `user_id` được truyền vào sẽ lấy theo `user_id`, nếu không sẽ lấy theo user đang đăng nhập.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetUnread(
        [FromQuery(Name = "user_id")] string? userId,
        CancellationToken cancellationToken)
    {
        var targetUserId = !string.IsNullOrWhiteSpace(userId) ? userId : _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return Ok(new List<NotificationResponse>());
        }

        var result = await _notificationService.GetUnreadByUserIdAsync(targetUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới một thông báo thủ công (hoặc thông qua service/tích hợp).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationResponse>> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _notificationService.CreateNotificationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUnread), new { user_id = result.UserId }, result);
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// </summary>
    [HttpPut("{id:long}/read")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] long id,
        [FromQuery(Name = "user_id")] string? userId,
        CancellationToken cancellationToken)
    {
        var targetUserId = !string.IsNullOrWhiteSpace(userId) ? userId : _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return BadRequest(new { message = "UserId là bắt buộc." });
        }

        var success = await _notificationService.MarkAsReadAsync(id, targetUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Không tìm thấy thông báo ID = {id} của user {targetUserId}." });
        }

        return Ok(new { message = "Đã đánh dấu thông báo là đã đọc." });
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng là đã đọc.
    /// </summary>
    [HttpPut("read-all")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(
        [FromQuery(Name = "user_id")] string? userId,
        CancellationToken cancellationToken)
    {
        var targetUserId = !string.IsNullOrWhiteSpace(userId) ? userId : _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return BadRequest(new { message = "UserId là bắt buộc." });
        }

        await _notificationService.MarkAllAsReadAsync(targetUserId, cancellationToken);
        return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc." });
    }
}
