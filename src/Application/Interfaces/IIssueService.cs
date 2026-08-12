using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueService
{
    Task<ApiResponse<System.Collections.Generic.IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, CancellationToken ct = default);
}
