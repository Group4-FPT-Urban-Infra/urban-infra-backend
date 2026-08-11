using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.Departments;

public class CreateDepartmentRequest
{
    public int? ParentDepartmentId { get; set; }

    [Required, MaxLength(30)]
    public string DepartmentCode { get; set; } = default!;

    [Required, MaxLength(200)]
    public string DepartmentName { get; set; } = default!;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateDepartmentRequest : CreateDepartmentRequest;

public class DepartmentFilterRequest
{
    public int? ParentDepartmentId { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

public class DepartmentResponse
{
    public int DepartmentId { get; set; }
    public int? ParentDepartmentId { get; set; }
    public string? ParentDepartmentName { get; set; }
    public string DepartmentCode { get; set; } = default!;
    public string DepartmentName { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ManagerId { get; set; }
    public string? ManagerFullName { get; set; }
    public string? ManagerEmail { get; set; }
    public List<DepartmentResponse> ChildDepartments { get; set; } = new();
}

public class AssignDepartmentMemberRequest
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = default!;

    [MaxLength(120)]
    public string? JobTitle { get; set; }

    public bool IsManager { get; set; }
}

public class DepartmentMemberResponse
{
    public int DepartmentId { get; set; }
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Email { get; set; }
    public string? JobTitle { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; }
}
