using UrbanInfraSystem.Application.DTOs.Departments;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentResponse>> GetDepartmentsAsync(DepartmentFilterRequest filter, CancellationToken cancellationToken = default);
    Task<DepartmentResponse?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default);
}
