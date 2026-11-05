using Audit.Contracts;

namespace Audit.Infrastructure.Queues;

public interface IAuditQueueSender
{
    Task SendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}