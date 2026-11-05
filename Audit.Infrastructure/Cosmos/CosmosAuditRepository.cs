using Audit.Contracts;
using Audit.Infrastructure.Tenancy;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Audit.Infrastructure.Cosmos;

//Create each Cosmos container with partition key `/packageId`.
//Keep tenant-to-container mapping server-controlled.
//Do not accept a container name from the HTTP request.
public sealed class CosmosAuditRepository(
    CosmosClient cosmos,
    IConfiguration configuration,
    ITenantContainerResolver tenantResolver)  : ICosmosAuditRepository
{
    public async Task CreateIfAbsentAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var databaseName = configuration["Cosmos:DatabaseName"]
                           ?? throw new InvalidOperationException("Cosmos database name missing.");

        var containerName = tenantResolver.ResolveContainerName(auditEvent.ClientId);
        var container = cosmos.GetDatabase(databaseName).GetContainer(containerName);

        var document = new
        {
            id = auditEvent.EventId,
            eventId = auditEvent.EventId,
            auditEvent.SchemaVersion,
            auditEvent.EventType,
            auditEvent.OccurredAtUtc,
            auditEvent.ClientId,
            auditEvent.PackageId,
            auditEvent.ActorId,
            auditEvent.ActorName,
            auditEvent.CorrelationId,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.QuoteRef,
            auditEvent.PolicyNumber,
            auditEvent.Action,
            auditEvent.Outcome,
            auditEvent.MetaData
        };

        try
        {
            await container.CreateItemAsync(
                document,
                new PartitionKey(auditEvent.PackageId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Duplicate event delivery: same id in this tenant container/partition.
            // For strict integrity, read the existing document and compare its eventId
            // and immutable content before treating this as an acknowledged duplicate.
        }
    }
}