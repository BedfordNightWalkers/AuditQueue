using System.Net;
using Audit.Contracts;
using Audit.Infrastructure.Cosmos;
using Audit.Infrastructure.Tenancy;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Audit.Tests.Cosmos;

public sealed class CosmosAuditRepositoryTests
{
    private const string DatabaseName = "audit-db";
    private const string ContainerName = "tenant-a-audit";

    private readonly Mock<CosmosClient> _cosmos = new();
    private readonly Mock<Database> _database = new();
    private readonly Mock<Container> _container = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ITenantContainerResolver> _tenantResolver = new();

    private readonly CosmosAuditRepository _sut;

    public CosmosAuditRepositoryTests()
    {
        _configuration.Setup(c => c["Cosmos:DatabaseName"]).Returns(DatabaseName);
        _tenantResolver.Setup(r => r.ResolveContainerName(It.IsAny<string>())).Returns(ContainerName);
        _cosmos.Setup(c => c.GetDatabase(DatabaseName)).Returns(_database.Object);
        _database.Setup(d => d.GetContainer(ContainerName)).Returns(_container.Object);

        _sut = new CosmosAuditRepository(_cosmos.Object, _configuration.Object, _tenantResolver.Object);
    }

    [Fact]
    public async Task CreateIfAbsentAsync_ResolvesContainerFromTenantClientId()
    {
        var auditEvent = CreateAuditEvent();

        await _sut.CreateIfAbsentAsync(auditEvent, CancellationToken.None);

        _tenantResolver.Verify(r => r.ResolveContainerName("client-a"), Times.Once);
        _cosmos.Verify(c => c.GetDatabase(DatabaseName), Times.Once);
        _database.Verify(d => d.GetContainer(ContainerName), Times.Once);
    }

    [Fact]
    public async Task CreateIfAbsentAsync_CreatesItemWithPackageIdPartitionKeyAndCancellationToken()
    {
        var auditEvent = CreateAuditEvent();
        using var cts = new CancellationTokenSource();

        await _sut.CreateIfAbsentAsync(auditEvent, cts.Token);

        var invocation = Assert.Single(_container.Invocations, i => i.Method.Name == nameof(Container.CreateItemAsync));
        Assert.Equal(new PartitionKey("package-1"), invocation.Arguments[1]);
        Assert.Equal(cts.Token, invocation.Arguments[3]);
    }

    [Fact]
    public async Task CreateIfAbsentAsync_MapsAuditEventToDocumentUsingEventIdAsId()
    {
        var auditEvent = CreateAuditEvent();

        await _sut.CreateIfAbsentAsync(auditEvent, CancellationToken.None);

        var document = Assert.Single(_container.Invocations, i => i.Method.Name == nameof(Container.CreateItemAsync))
            .Arguments[0];

        Assert.Equal(auditEvent.EventId, GetProperty(document, "id"));
        Assert.Equal(auditEvent.EventId, GetProperty(document, "eventId"));
        Assert.Equal(auditEvent.SchemaVersion, GetProperty(document, nameof(AuditEvent.SchemaVersion)));
        Assert.Equal(auditEvent.EventType, GetProperty(document, nameof(AuditEvent.EventType)));
        Assert.Equal(auditEvent.OccurredAtUtc, GetProperty(document, nameof(AuditEvent.OccurredAtUtc)));
        Assert.Equal(auditEvent.ClientId, GetProperty(document, nameof(AuditEvent.ClientId)));
        Assert.Equal(auditEvent.PackageId, GetProperty(document, nameof(AuditEvent.PackageId)));
        Assert.Equal(auditEvent.ActorId, GetProperty(document, nameof(AuditEvent.ActorId)));
        Assert.Equal(auditEvent.ActorName, GetProperty(document, nameof(AuditEvent.ActorName)));
        Assert.Equal(auditEvent.CorrelationId, GetProperty(document, nameof(AuditEvent.CorrelationId)));
        Assert.Equal(auditEvent.EntityType, GetProperty(document, nameof(AuditEvent.EntityType)));
        Assert.Equal(auditEvent.EntityId, GetProperty(document, nameof(AuditEvent.EntityId)));
        Assert.Equal(auditEvent.QuoteRef, GetProperty(document, nameof(AuditEvent.QuoteRef)));
        Assert.Equal(auditEvent.PolicyNumber, GetProperty(document, nameof(AuditEvent.PolicyNumber)));
        Assert.Equal(auditEvent.Action, GetProperty(document, nameof(AuditEvent.Action)));
        Assert.Equal(auditEvent.Outcome, GetProperty(document, nameof(AuditEvent.Outcome)));
        Assert.Same(auditEvent.MetaData, GetProperty(document, nameof(AuditEvent.MetaData)));
    }

    [Fact]
    public async Task CreateIfAbsentAsync_WhenItemAlreadyExists_SwallowsConflict()
    {
        SetupCreateItemToThrow(HttpStatusCode.Conflict);

        var exception = await Record.ExceptionAsync(
            () => _sut.CreateIfAbsentAsync(CreateAuditEvent(), CancellationToken.None));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task CreateIfAbsentAsync_WhenCosmosFailsWithNonConflict_Rethrows(HttpStatusCode statusCode)
    {
        SetupCreateItemToThrow(statusCode);

        var exception = await Assert.ThrowsAsync<CosmosException>(
            () => _sut.CreateIfAbsentAsync(CreateAuditEvent(), CancellationToken.None));

        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Fact]
    public async Task CreateIfAbsentAsync_WhenDatabaseNameMissing_ThrowsWithoutTouchingCosmos()
    {
        _configuration.Setup(c => c["Cosmos:DatabaseName"]).Returns((string?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateIfAbsentAsync(CreateAuditEvent(), CancellationToken.None));

        Assert.Equal("Cosmos database name missing.", exception.Message);
        _tenantResolver.Verify(r => r.ResolveContainerName(It.IsAny<string>()), Times.Never);
        _cosmos.Verify(c => c.GetDatabase(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateIfAbsentAsync_WhenTenantHasNoContainer_PropagatesAndDoesNotWrite()
    {
        _tenantResolver
            .Setup(r => r.ResolveContainerName("client-a"))
            .Throws(new InvalidOperationException("No audit container configured for client 'client-a'"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateIfAbsentAsync(CreateAuditEvent(), CancellationToken.None));

        _cosmos.Verify(c => c.GetDatabase(It.IsAny<string>()), Times.Never);
        Assert.Empty(_container.Invocations);
    }

    private void SetupCreateItemToThrow(HttpStatusCode statusCode)
    {
        _container
            .Setup(c => c.CreateItemAsync(
                It.IsAny<It.IsAnyType>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Throws(new CosmosException("cosmos failure", statusCode, 0, "activity-id", 0));
    }

    private static object? GetProperty(object document, string name)
    {
        var property = document.GetType().GetProperty(name);
        Assert.True(property is not null, $"Document has no property '{name}'.");
        return property.GetValue(document);
    }

    private static AuditEvent CreateAuditEvent() => new()
    {
        EventId = "evt-123",
        SchemaVersion = 2,
        EventType = "Policy.StatusChanged",
        OccurredAtUtc = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero),
        ClientId = "client-a",
        PackageId = "package-1",
        ActorId = "user-42",
        ActorName = "Jane Doe",
        CorrelationId = "corr-789",
        EntityType = "Policy",
        EntityId = "policy-1",
        QuoteRef = "Q-100",
        PolicyNumber = "P-200",
        Action = "StatusChanged",
        Outcome = "Success",
        MetaData = new Dictionary<string, string> { ["from"] = "Draft", ["to"] = "Active" }
    };
}
