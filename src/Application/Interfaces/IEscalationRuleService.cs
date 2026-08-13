using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.EscalationRules;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IEscalationRuleService
{
    Task<IReadOnlyList<EscalationRuleResponse>> GetAllAsync(CancellationToken ct = default);

    Task<EscalationRuleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<EscalationRuleResponse> CreateAsync(CreateEscalationRuleRequest request, CancellationToken ct = default);

    Task<EscalationRuleResponse?> UpdateAsync(Guid id, UpdateEscalationRuleRequest request, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
