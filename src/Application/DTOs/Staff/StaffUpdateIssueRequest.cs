using Microsoft.AspNetCore.Http;

namespace UrbanInfraSystem.Application.DTOs.Staff;

/// <summary>
/// Request body kèm file ảnh để staff cập nhật tình trạng sự cố và tải lên bằng chứng xử lý.
/// </summary>
public class StaffUpdateIssueRequest
{
    /// <summary>
    /// ID trạng thái mới cần chuyển sang. Nếu null thì giữ nguyên trạng thái hiện tại.
    /// </summary>
    public int? StatusId { get; set; }

    /// <summary>
    /// Ghi chú / mô tả cập nhật của cán bộ.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Các file ảnh bằng chứng tải lên (multipart/form-data).
    /// </summary>
    public List<IFormFile>? Images { get; set; }
}
