namespace UrbanInfraSystem.Application.DTOs.Dashboard;

/// <summary>Một điểm dữ liệu trong biểu đồ xu hướng sự cố.</summary>
public class TrendDataPoint
{
    /// <summary>Nhãn trục X: "Mon"/"Tue"/... (tuần) · "1"/"2"/... (tháng) · "Jan"/"Feb"/... (năm).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Số sự cố mới được báo cáo trong khoảng thời gian tương ứng.</summary>
    public int Value { get; set; }
}
