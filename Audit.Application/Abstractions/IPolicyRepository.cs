using Audit.Application.Policies;

namespace Audit.Application.Abstractions;

public interface IPolicyRepository
{
    Task<PolicyRecord?> GetByIdAsync(
        Guid policyId,
        string clientId,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        Guid policyId,
        string clientId,
        string newStatus,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken);

}