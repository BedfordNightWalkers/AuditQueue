namespace Audit.Infrastructure.Data.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    
    public required string EventId { get; set; }
    
    public required string EventType { get; set; }
    
    public required string PayloadJson { get; set; }
    
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? PublishedAtUtc { get; set; }
    
    public int Attempts { get; set; }
    
    public string? LastError  { get; set; }
}