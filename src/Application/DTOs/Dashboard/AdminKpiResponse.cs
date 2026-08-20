namespace UrbanInfraSystem.Application.DTOs.Dashboard;

/// <summary>Một chỉ số KPI kèm giá trị hiện tại và xu hướng so với tuần trước.</summary>
public class KpiItem
{
    /// <summary>Giá trị hiện tại.</summary>
    public int Value { get; set; }

    /// <summary>Phần trăm thay đổi so với kỳ trước. Null nếu không có dữ liệu so sánh.</summary>
    public double? TrendPercent { get; set; }

    /// <summary>Hướng xu hướng: "up" | "down" | "stable".</summary>
    public string TrendDirection { get; set; } = "stable";
}

/// <summary>Toàn bộ dữ liệu KPI dành riêng cho Admin Dashboard.</summary>
public class AdminKpiResponse
{
    /// <summary>Tổng số người dùng đang hoạt động (IsActive = true).</summary>
    public KpiItem TotalUsers { get; set; } = new();

    /// <summary>Số sự cố đang mở (chưa Resolved / Closed / Rejected).</summary>
    public KpiItem OpenIncidents { get; set; } = new();

    /// <summary>Số phòng ban / đơn vị đang hoạt động (IsActive = true).</summary>
    public KpiItem ActiveDepartments { get; set; } = new();

    /// <summary>Số sự cố đã giải quyết trong tuần hiện tại.</summary>
    public KpiItem ResolvedThisWeek { get; set; } = new();

    /// <summary>Số sự cố mới được báo cáo trong hôm nay (so sánh trend với hôm qua).</summary>
    public KpiItem NewToday { get; set; } = new();

    /// <summary>Số sự cố Critical đang mở.</summary>
    public KpiItem CriticalIncidents { get; set; } = new();
}
