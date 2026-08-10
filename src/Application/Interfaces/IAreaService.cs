using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Areas;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IAreaService
{
    Task<List<AreaResponse>> GetAreasAsync(AreaFilterRequest filter, CancellationToken cancellationToken = default);

    Task<AreaResponse?> GetAreaByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default);

    Task<AreaResponse> UpdateAreaAsync(int id, UpdateAreaRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAreaAsync(int id, CancellationToken cancellationToken = default);
}
