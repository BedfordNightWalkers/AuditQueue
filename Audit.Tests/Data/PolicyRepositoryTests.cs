using Audit.Infrastructure.Data;
using Audit.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audit.Tests.Data;

public sealed class PolicyRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset OriginalUpdatedAtUtc = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NewUpdatedAtUtc = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteAuditDatabase _database = new();
    private readonly AuditDbContext _dbContext;
    private readonly PolicyRepository _sut;

    private readonly Policy _policy = CreatePolicy("client-a", "POL-001");
    private readonly Policy _otherTenantPolicy = CreatePolicy("client-b", "POL-002");

    public PolicyRepositoryTests()
    {
        using (var seedContext = _database.CreateContext())
        {
            seedContext.Policies.AddRange(_policy, _otherTenantPolicy);
            seedContext.SaveChanges();
        }

        _dbContext = _database.CreateContext();
        _sut = new PolicyRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _database.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_MatchingIdAndClient_ReturnsMappedRecord()
    {
        var record = await _sut.GetByIdAsync(_policy.Id, "client-a", CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal(_policy.Id, record.Id);
        Assert.Equal("client-a", record.ClientId);
        Assert.Equal(_policy.PackageId, record.PackageId);
        Assert.Equal(_policy.QuoteRef, record.QuoteRef);
        Assert.Equal("POL-001", record.PolicyNumber);
        Assert.Equal("Draft", record.Status);
        Assert.Equal(OriginalUpdatedAtUtc, record.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetByIdAsync_PolicyBelongsToAnotherClient_ReturnsNull()
    {
        var record = await _sut.GetByIdAsync(_otherTenantPolicy.Id, "client-a", CancellationToken.None);

        Assert.Null(record);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var record = await _sut.GetByIdAsync(Guid.NewGuid(), "client-a", CancellationToken.None);

        Assert.Null(record);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotTrackEntities()
    {
        await _sut.GetByIdAsync(_policy.Id, "client-a", CancellationToken.None);

        Assert.Empty(_dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UpdateStatusAsync_MatchingIdAndClient_ModifiesTrackedPolicy()
    {
        await _sut.UpdateStatusAsync(_policy.Id, "client-a", "Active", NewUpdatedAtUtc, CancellationToken.None);

        var entry = Assert.Single(_dbContext.ChangeTracker.Entries<Policy>());
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal("Active", entry.Entity.Status);
        Assert.Equal(NewUpdatedAtUtc, entry.Entity.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotSaveChanges()
    {
        await _sut.UpdateStatusAsync(_policy.Id, "client-a", "Active", NewUpdatedAtUtc, CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        var stored = await verifyContext.Policies.SingleAsync(p => p.Id == _policy.Id);
        Assert.Equal("Draft", stored.Status);
        Assert.Equal(OriginalUpdatedAtUtc, stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusAsync_ThenSaveChanges_PersistsOnlyTargetPolicy()
    {
        await _sut.UpdateStatusAsync(_policy.Id, "client-a", "Active", NewUpdatedAtUtc, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        await using var verifyContext = _database.CreateContext();
        var updated = await verifyContext.Policies.SingleAsync(p => p.Id == _policy.Id);
        var untouched = await verifyContext.Policies.SingleAsync(p => p.Id == _otherTenantPolicy.Id);

        Assert.Equal("Active", updated.Status);
        Assert.Equal(NewUpdatedAtUtc, updated.UpdatedAtUtc);
        Assert.Equal("Draft", untouched.Status);
        Assert.Equal(OriginalUpdatedAtUtc, untouched.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusAsync_PolicyBelongsToAnotherClient_ThrowsAndLeavesPolicyUnchanged()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(_otherTenantPolicy.Id, "client-a", "Active", NewUpdatedAtUtc, CancellationToken.None));

        Assert.Equal("Policy not found", exception.Message);
        Assert.Empty(_dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownId_Throws()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(Guid.NewGuid(), "client-a", "Active", NewUpdatedAtUtc, CancellationToken.None));

        Assert.Equal("Policy not found", exception.Message);
    }

    private static Policy CreatePolicy(string clientId, string policyNumber) => new()
    {
        ClientId = clientId,
        PackageId = "package-1",
        QuoteRef = "Q12345",
        PolicyNumber = policyNumber,
        Status = "Draft",
        UpdatedAtUtc = OriginalUpdatedAtUtc
    };
}
