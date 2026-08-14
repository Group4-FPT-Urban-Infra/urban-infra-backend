using UrbanInfraSystem.Application.DTOs.DepartmentManager;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentManagerStaffService
{
    Task<IReadOnlyList<StaffMemberResponse>> GetStaffsAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<StaffDetailResponse?> GetStaffDetailAsync(string userId, CancellationToken cancellationToken = default);
}
