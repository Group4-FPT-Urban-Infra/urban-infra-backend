using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Notifications;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Hubs;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub>? _hubContext;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub>? hubContext = null)
    {
        _db = db;
        _hubContext = hubContext;
    }

    public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        var entity = new Notification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            NotificationType = string.IsNullOrWhiteSpace(request.NotificationType) ? "CUSTOM" : request.NotificationType.Trim(),
            IssueId = request.IssueId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync(ct);

        var response = Map(entity);

        // Broadcast realtime notification via SignalR Hub if hubContext is available
        if (_hubContext != null)
        {
            try
            {
                await _hubContext.Clients.Group(request.UserId).SendAsync("ReceiveNotification", response, cancellationToken: ct);
            }
            catch
            {
                // Silence hub broadcast errors to prevent blocking DB operations
            }
        }

        return response;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetUnreadByUserIdAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<NotificationResponse>();
        }

        var items = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, string userId, CancellationToken ct = default)
    {
        var entity = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
        if (entity is null) return false;

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var unreadNotifications = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (!unreadNotifications.Any()) return true;

        var now = DateTime.UtcNow;
        foreach (var item in unreadNotifications)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static NotificationResponse Map(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        NotificationType = n.NotificationType,
        IssueId = n.IssueId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}
