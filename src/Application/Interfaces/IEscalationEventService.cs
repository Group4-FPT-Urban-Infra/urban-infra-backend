using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.EscalationEvents;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IEscalationEventService
{
    Task<IReadOnlyList<EscalationEventResponse>> GetByIssueAsync(long issueId, CancellationToken ct = default);

    Task<IReadOnlyList<EscalationEventResponse>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default);

    Task<EscalationEventResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<EscalationEventResponse> AcknowledgeAsync(Guid id, string currentUserId, CancellationToken ct = default);
}
