namespace UrbanInfraSystem.Application.DTOs.Dashboard;

/// <summary>
/// Dữ liệu phân bổ danh mục sự cố.
/// </summary>
public class CategoryDistributionPoint
{
    /// <summary>Tên danh mục (IssueType).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Số lượng sự cố thuộc danh mục này.</summary>
    public int Count { get; set; }

    /// <summary>Tỷ lệ phần trăm so với tổng số (0-100).</summary>
    public double Percentage { get; set; }
}
