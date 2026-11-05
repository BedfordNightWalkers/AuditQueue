namespace Audit.Application.Policies;

public sealed record PolicyRecord(
    Guid Id,
    string ClientId,
    string PackageId,
    string QuoteRef,
    string PolicyNumber,
    string Status,
    DateTimeOffset UpdatedAtUtc);