using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.SlaPolicies;

namespace UrbanInfraSystem.Application.Interfaces;

public interface ISlaPolicyService
{
    Task<IReadOnlyList<SlaPolicyResponse>> GetAllAsync(CancellationToken ct = default);

    Task<SlaPolicyResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SlaPolicyResponse> CreateAsync(CreateSlaPolicyRequest request, CancellationToken ct = default);

    Task<SlaPolicyResponse?> UpdateAsync(Guid id, UpdateSlaPolicyRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
