using System.Text.Json;
using Audit.Application.Abstractions;
using Audit.Contracts;

namespace Audit.Application.Policies;

public sealed class PolicyService(
    IPolicyRepository policies,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork): IPolicyService
{

    private static readonly HashSet<string> AllowedStatuses =
        new (StringComparer.OrdinalIgnoreCase)
        {
            "Quoted", "Bound", "Cancelled"
        };

    public async Task<PolicyDto?> ChangeStatusAsync(
        Guid policyId,
        string trustedClientId,
        string actorId,
        string? actorName,
        string correlationId,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var status = newStatus.Trim();
        if(!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Unsupported policy status.", nameof(newStatus));
        }

        PolicyRecord? policy = null;
        var now = DateTimeOffset.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            policy = await policies.GetByIdAsync(
                policyId,
                trustedClientId,
                ct);

            if(policy is null)
                return;

            if (policy.Status.Equals(
                    status,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (policy.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "A cancelled policy cannot transition into another status.");

            await policies.UpdateStatusAsync(policyId, trustedClientId, status, now, ct);

            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                SchemaVersion = 1,
                EventType = "Policy.StatusChanged",
                OccurredAtUtc = now,
                ClientId = policy.ClientId,
                PackageId = policy.PackageId,
                ActorId = actorId,
                ActorName = actorName,
                CorrelationId = correlationId,
                EntityType = "Policy",
                EntityId = policy.Id.ToString(),
                QuoteRef = policy.QuoteRef,
                PolicyNumber = policy.PolicyNumber,
                Action = "StatusChanged",
                Outcome = "Success",
                MetaData = new Dictionary<string, string>()
                {
                    ["oldStatus"] = policy.Status,
                    ["newStatus"] = status
                }
            };

            await outbox.AddAsync(
                auditEvent.EventId,
                auditEvent.EventType,
                JsonSerializer.Serialize(auditEvent),
                now,
                ct);

        }, cancellationToken);

        return policy is null
            ? null
            : new PolicyDto(policy.Id, policy.ClientId, policy.PackageId,
                policy.QuoteRef, policy.PolicyNumber, status, now);
    }
}