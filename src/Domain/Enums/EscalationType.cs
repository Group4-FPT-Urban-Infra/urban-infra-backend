namespace UrbanInfraSystem.Domain.Enums;

/// <summary>
/// Loại escalation trigger cho SLA.
/// </summary>
public enum EscalationType
{
    /// <summary>
    /// Quá hạn phản hồi đầu tiên (First Response)
    /// </summary>
    FirstResponseOverdue = 1,

    /// <summary>
    /// Sắp đến hạn phản hồi/cần chú ý
    /// </summary>
    ApproachResponseDeadline = 2,

    /// <summary>
    /// Quá hạn giải quyết (Resolution)
    /// </summary>
    ResponseOverdue = 3
}
