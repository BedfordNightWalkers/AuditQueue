using System.Text;
using System.Text.Json;
using Audit.Contracts;
using Azure.Storage.Queues;

namespace Audit.Infrastructure.Queues;

public sealed class AuditQueueSender(QueueClient queue) : IAuditQueueSender
{
    public async Task SendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(auditEvent);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        await queue.SendMessageAsync(base64, cancellationToken);
    }
}