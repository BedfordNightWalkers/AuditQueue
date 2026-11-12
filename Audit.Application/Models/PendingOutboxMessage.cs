namespace Audit.Application.Models;

public sealed class PendingOutboxMessage
{
    public Guid Id { get; set; }

    public required string EventId { get; set; }

    public required string EventType { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}