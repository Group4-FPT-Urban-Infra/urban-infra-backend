namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity cho đính kèm của sự cố (hình ảnh, file).
/// </summary>
public class IssueAttachment
{
    /// <summary>ID tự tăng của đính kèm.</summary>
    public long Id { get; set; }

    /// <summary>ID của sự cố cha.</summary>
    public long IssueId { get; set; }

    /// <summary>ID của update mà đính kèm này gắn vào (nullable).</summary>
    public long? UpdateId { get; set; }

    /// <summary>ID của user tải lên.</summary>
    public string UploadedBy { get; set; } = default!;

    /// <summary>Loại đính kèm (image/video/document).</summary>
    public string Kind { get; set; } = default!;

    /// <summary>URL đầy đủ của file.</summary>
    public string FileUrl { get; set; } = default!;

    /// <summary>URL thumbnail (cho hình ảnh).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>MIME type của file.</summary>
    public string MimeType { get; set; } = default!;

    /// <summary>Kích thước file (bytes).</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Chiều rộng ảnh (pixels) - cho hình ảnh.</summary>
    public int? WidthPx { get; set; }

    /// <summary>Chiều cao ảnh (pixels) - cho hình ảnh.</summary>
    public int? HeightPx { get; set; }

    /// <summary>Thời điểm tải lên.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Sự cố cha.</summary>
    public Issue Issue { get; set; } = default!;

    /// <summary>Update mà đính kèm gắn vào.</summary>
    public IssueUpdate? Update { get; set; }
}
