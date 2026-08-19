using UrbanInfraSystem.Application.DTOs.IssueAssignments;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueAssignmentService
{
    Task<IReadOnlyList<IssueAssignmentResponse>?> GetHistoryAsync(long issueId, CancellationToken cancellationToken = default);
    Task<IssueAssignmentResponse> ReassignAsync(long issueId, ReassignIssueRequest request, string actorUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IssueAssignmentResponse> ManualRouteAsync(long issueId, ManualRouteRequest request, string adminUserId, CancellationToken cancellationToken = default);
}
