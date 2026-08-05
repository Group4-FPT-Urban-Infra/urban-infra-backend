namespace UrbanInfraSystem.Domain.Common;

/// <summary>
/// Base class cho các entity nghiệp vụ (không bao gồm Identity entities).
/// Cung cấp các trường audit chung phục vụ yêu cầu "ghi log đầy đủ các bước xử lý".
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
}
