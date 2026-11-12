using Audit.Application.Abstractions;
using Audit.Application.Models;
using Audit.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be greater than zero.");
        }
        
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.PublishedAtUtc == null)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(batchSize)
            .Select(message => new PendingOutboxMessage
            {
                Id = message.Id,
                EventId = message.EventId,
                EventType = message.EventType,
                PayloadJson = message.PayloadJson,
                CreatedAtUtc = message.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(
        Guid id,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(
                message => message.Id == id,
                cancellationToken);

        if (message == null)
        {
            throw new InvalidOperationException(
                $"Outbox message with id {id} not found.");
        }
        
        message.PublishedAtUtc = publishedAtUtc;
        message.LastError = null;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(
                message => message.Id == id,
                cancellationToken);

        if (message == null)
        {
            throw new InvalidOperationException(
                $"Outbox message with id {id} not found.");
        }
        
        message.Attempts++;

        //LastError has a maximum configured length of 2000 characters
        message.LastError = error.Length > 2000
            ? error[..2000]
            : error;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}