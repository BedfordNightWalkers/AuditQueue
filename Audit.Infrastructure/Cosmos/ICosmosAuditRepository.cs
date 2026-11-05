using Audit.Contracts;

namespace Audit.Infrastructure.Cosmos;

public interface ICosmosAuditRepository
{
    Task CreateIfAbsentAsync(AuditEvent auditEvent,  CancellationToken cancellationToken);
}