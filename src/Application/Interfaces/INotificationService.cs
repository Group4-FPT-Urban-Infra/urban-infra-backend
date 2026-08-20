using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Notifications;

namespace UrbanInfraSystem.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationResponse>> GetUnreadByUserIdAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationResponse>> GetAllByUserIdAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationResponse>> GetReadByUserIdAsync(string userId, CancellationToken ct = default);

    Task<bool> MarkAsReadAsync(long notificationId, string userId, CancellationToken ct = default);

    Task<bool> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
