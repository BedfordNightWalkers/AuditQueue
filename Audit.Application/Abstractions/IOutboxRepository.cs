using Audit.Application.Models;

namespace Audit.Application.Abstractions;

public interface IOutboxRepository
{
    Task AddAsync(
        string eventId,
        string eventType,
        string payloadJson,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken);
    
    Task MarkPublishedAsync(
        Guid id,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken);

    Task RecordFailureAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken);
}