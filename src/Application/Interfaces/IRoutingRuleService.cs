using UrbanInfraSystem.Application.DTOs.RoutingRules;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IRoutingRuleService
{
    Task<IReadOnlyList<RoutingRuleResponse>> GetAllAsync(RoutingRuleFilterRequest filter, CancellationToken cancellationToken = default);
    Task<RoutingRuleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RoutingRuleResponse?> ResolveAsync(int issueTypeId, int areaId, CancellationToken cancellationToken = default);
    Task<RoutingRuleResponse> CreateAsync(CreateRoutingRuleRequest request, CancellationToken cancellationToken = default);
    Task<RoutingRuleResponse?> UpdateAsync(int id, UpdateRoutingRuleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
