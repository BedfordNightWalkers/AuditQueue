namespace Audit.Application.Policies;

public interface IPolicyService
{
    Task<PolicyDto?> ChangeStatusAsync(
        Guid policyId,
        string trustedClientId,
        string actorId,
        string? actorName,
        string correlationId,
        string newStatus,
        CancellationToken cancellationToken);
}