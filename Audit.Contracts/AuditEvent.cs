namespace Audit.Contracts;

public sealed record AuditEvent
{
    // Stable ID generated once by the producer, reused on every retry.
    public required string EventId { get; init;}

    // Enables safe evolution of the event shape.
    public int SchemaVersion { get; init; } = 1;

    public required string EventType { get; init; } //e.g. Policy.StatusChanged

    public required DateTimeOffset OccurredAtUtc { get; init; }

    // Tenant identity comes from trusted server-side context
    public required string ClientId { get; init; }
    public required string PackageId { get; init; }

    public required string ActorId { get; init; }
    public string? ActorName { get; init; }
    public required string CorrelationId { get; init; }

    public required string EntityType { get; init; } // e.g. Policy
    public required string EntityId { get; init; }
    public string? QuoteRef { get;  init; }
    public string? PolicyNumber { get;  init; }

    public string? Action { get; init; } // e.g. StatusChanged
    public string? Outcome { get; init; } // Success / Failure
    public Dictionary<string, string>? MetaData {get; init; }

}