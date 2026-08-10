using UrbanInfraSystem.Application.DTOs.Departments;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentMemberService
{
    Task<List<DepartmentMemberResponse>> GetMembersAsync(int departmentId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<DepartmentMemberResponse> AssignMemberAsync(int departmentId, AssignDepartmentMemberRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(int departmentId, string userId, CancellationToken cancellationToken = default);
}
