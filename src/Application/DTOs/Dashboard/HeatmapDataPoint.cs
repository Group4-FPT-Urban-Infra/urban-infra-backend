namespace UrbanInfraSystem.Application.DTOs.Dashboard;

/// <summary>
/// Dữ liệu cụm sự cố cho bản đồ mật độ (Heatmap).
/// </summary>
public class HeatmapDataPoint
{
    /// <summary>Mã khu vực (ví dụ: Q1, BC, HG...).</summary>
    public string DistrictId { get; set; } = string.Empty;

    /// <summary>Tên khu vực hiển thị trên bản đồ.</summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>Mức độ nghiêm trọng tổng thể của cụm (VD: Critical, High, Medium, Low).</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Số lượng sự cố trong cụm này.</summary>
    public int Count { get; set; }
}
