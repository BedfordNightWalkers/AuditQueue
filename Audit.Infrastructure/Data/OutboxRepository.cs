using Audit.Application.Abstractions;
using Audit.Infrastructure.Data.Entities;

namespace Audit.Infrastructure.Data;

public sealed class OutboxRepository(AuditDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(
        string eventId,
        string eventType,
        string payloadJson,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        await dbContext.OutboxMessages.AddAsync(
            new OutboxMessage
            {
                EventId = eventId,
                EventType = eventType,
                PayloadJson = payloadJson,
                CreatedAtUtc = createdAtUtc,
                Attempts = 0
            },
            cancellationToken);
    }
}