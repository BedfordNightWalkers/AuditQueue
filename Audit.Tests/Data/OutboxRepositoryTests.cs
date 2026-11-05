using Audit.Infrastructure.Data;
using Audit.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audit.Tests.Data;

public sealed class OutboxRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteAuditDatabase _database = new();
    private readonly AuditDbContext _dbContext;
    private readonly OutboxRepository _sut;

    public OutboxRepositoryTests()
    {
        _dbContext = _database.CreateContext();
        _sut = new OutboxRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _database.Dispose();
    }

    [Fact]
    public async Task AddAsync_TracksNewOutboxMessageWithMappedFields()
    {
        await _sut.AddAsync("evt-123", "Policy.StatusChanged", """{"status":"Active"}""", CreatedAtUtc, CancellationToken.None);

        var entry = Assert.Single(_dbContext.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(EntityState.Added, entry.State);

        var message = entry.Entity;
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("evt-123", message.EventId);
        Assert.Equal("Policy.StatusChanged", message.EventType);
        Assert.Equal("""{"status":"Active"}""", message.PayloadJson);
        Assert.Equal(CreatedAtUtc, message.CreatedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.PublishedAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task AddAsync_DoesNotSaveChanges()
    {
        await _sut.AddAsync("evt-123", "Policy.StatusChanged", "{}", CreatedAtUtc, CancellationToken.None);

        await using var verifyContext = _database.CreateContext();
        Assert.Empty(await verifyContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsPendingMessage()
    {
        await _sut.AddAsync("evt-123", "Policy.StatusChanged", """{"status":"Active"}""", CreatedAtUtc, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        await using var verifyContext = _database.CreateContext();
        var message = Assert.Single(await verifyContext.OutboxMessages.ToListAsync());
        Assert.Equal("evt-123", message.EventId);
        Assert.Equal("Policy.StatusChanged", message.EventType);
        Assert.Equal("""{"status":"Active"}""", message.PayloadJson);
        Assert.Equal(CreatedAtUtc, message.CreatedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.PublishedAtUtc);
    }

    [Fact]
    public async Task AddAsync_MultipleEvents_AssignsDistinctIds()
    {
        await _sut.AddAsync("evt-1", "Policy.StatusChanged", "{}", CreatedAtUtc, CancellationToken.None);
        await _sut.AddAsync("evt-2", "Policy.StatusChanged", "{}", CreatedAtUtc, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        await using var verifyContext = _database.CreateContext();
        var ids = await verifyContext.OutboxMessages.Select(m => m.Id).ToListAsync();
        Assert.Equal(2, ids.Distinct().Count());
    }

    [Fact]
    public async Task AddAsync_DuplicateEventId_IsRejectedOnSave()
    {
        await _sut.AddAsync("evt-123", "Policy.StatusChanged", "{}", CreatedAtUtc, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        await _sut.AddAsync("evt-123", "Policy.StatusChanged", "{}", CreatedAtUtc, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
    }
}
