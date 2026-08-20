using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
using UrbanInfraSystem.Application.DTOs.ReRouteRequests;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IReRouteRequestService
{
    Task<ReRouteRequestDto> CreateRequestAsync(long issueId, ReassignIssueRequest request, string actorUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReRouteRequestDto>> GetIncomingRequestsAsync(int targetDepartmentId, CancellationToken cancellationToken = default);
    Task<ReRouteRequestDto?> GetPendingRequestByIssueIdAsync(long issueId, CancellationToken cancellationToken = default);
    Task<bool> AcceptRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default);
    Task<bool> RejectRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelRequestAsync(long requestId, string actorUserId, CancellationToken cancellationToken = default);
}
