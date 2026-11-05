namespace Audit.Application.Abstractions;

public interface IOutboxRepository
{
    Task AddAsync(
        string eventId,
        string eventType,
        string payloadJson,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);

}