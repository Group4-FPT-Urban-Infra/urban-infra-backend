using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace UrbanInfraSystem.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub truyền thông báo thời gian thực (Real-time Notifications) cho client.
/// Client kết nối qua route `/hubs/notifications`.
/// </summary>
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.Request.Query["user_id"].ToString();

        if (string.IsNullOrWhiteSpace(userId) && Context.UserIdentifier != null)
        {
            userId = Context.UserIdentifier;
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Cho phép client chủ động tham gia vào group nhận thông báo theo userId.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }

    /// <summary>
    /// Cho phép client rời khỏi group nhận thông báo.
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
    }
}
