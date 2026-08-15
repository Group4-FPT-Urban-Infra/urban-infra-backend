using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Bảng lưu nhật ký các hành động quan trọng trong hệ thống.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [MaxLength(50)]
    public string Action { get; set; } = default!;

    [MaxLength(100)]
    public string EntityName { get; set; } = default!;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public Guid? CorrelationId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
