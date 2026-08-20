namespace UrbanInfraSystem.Domain.Enums;

/// <summary>
/// 3 nhóm actor theo mục III của đặc tả: Quản trị viên, Cán bộ xử lý, Công dân.
/// Dùng làm tên Role trong ASP.NET Core Identity (RoleManager sẽ seed các role này).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string DepartmentStaff = "DepartmentStaff";
    public const string DepartmentManager = "DepartmentManager";
    public const string Citizen = "Citizen";

    public static readonly string[] All = { Admin, DepartmentStaff, DepartmentManager, Citizen };
}
