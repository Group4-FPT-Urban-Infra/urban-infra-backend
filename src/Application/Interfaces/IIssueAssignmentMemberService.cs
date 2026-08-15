using UrbanInfraSystem.Application.DTOs.IssueAssignmentMembers;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueAssignmentMemberService
{
    Task<IReadOnlyList<IssueAssignmentMemberResponse>?> GetByAssignmentAsync(
        long assignmentId,
        CancellationToken cancellationToken = default);

    Task<IssueAssignmentMemberResponse?> GetMemberDetailAsync(
        long memberId,
        CancellationToken cancellationToken = default);

    Task<IssueAssignmentMemberResponse> AssignMemberAsync(
        long assignmentId,
        AssignMemberRequest request,
        string assignedBy,
        CancellationToken cancellationToken = default);

    Task<IssueAssignmentMemberResponse> UpdateMemberStatusAsync(
        long memberId,
        UpdateMemberStatusRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(
        long memberId,
        string removedBy,
        CancellationToken cancellationToken = default);
}
