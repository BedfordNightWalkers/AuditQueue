namespace Audit.Infrastructure.Data.Entities;

public sealed class Policy
{
    public Guid Id { get; set; }  = Guid.NewGuid();
    public required string ClientId { get; set; }
    public required string PackageId { get; set; }
    public required string QuoteRef { get; set; }
    public required string PolicyNumber { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }  = DateTimeOffset.UtcNow;
}