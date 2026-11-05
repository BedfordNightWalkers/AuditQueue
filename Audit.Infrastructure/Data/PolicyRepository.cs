using Audit.Application.Abstractions;
using Audit.Application.Policies;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Data;

public sealed class PolicyRepository(AuditDbContext dbContext) : IPolicyRepository
{
    public Task<PolicyRecord?> GetByIdAsync(
        Guid policyId,
        string clientId,
        CancellationToken cancellationToken) =>
    dbContext.Policies.AsNoTracking()
        .Where(p => p.Id == policyId &&
                    p.ClientId == clientId)
        .Select(p => new PolicyRecord(
            p.Id, p.ClientId, p.PackageId,
            p.QuoteRef, p.PolicyNumber,
            p.Status, p.UpdatedAtUtc))
    .SingleOrDefaultAsync(cancellationToken);


    public async Task UpdateStatusAsync(
        Guid policyId,
        string clientId,
        string newStatus,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var policy = await dbContext.Policies.SingleOrDefaultAsync(
            p => p.Id == policyId &&
                 p.ClientId == clientId,
            cancellationToken);

        if(policy is null)
            throw new InvalidOperationException("Policy not found");

        policy.Status = newStatus;
        policy.UpdatedAtUtc = updatedAtUtc;
    }
}