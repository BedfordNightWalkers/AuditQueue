using Audit.Application.Abstractions;
using Audit.Infrastructure.Data;
using Audit.Infrastructure.Queues;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sql = configuration.GetConnectionString("AuditDb")
                  ?? throw new InvalidOperationException("AuditDb connection missing.");

        services.AddDbContext<AuditDbContext>(o => o.UseSqlServer(sql));

        var queueConnection = configuration["Queue:ConnectionString"]
            ?? "UseDevelopmentStorage=true";
        var queueName = configuration["Queue:QueueName"] ?? "audit-events";

        services.AddSingleton(_ => new QueueClient(queueConnection, queueName));
        services.AddScoped<IAuditQueueSender, AuditQueueSender>();

        // Cosmos DB
        var endpoint = configuration["Cosmos:Endpoint"]
            ?? throw new InvalidOperationException("Cosmos endpoint missing.");
        var key = configuration["Cosmos:Key"]
            ?? throw new InvalidOperationException("Cosmos key missing.");

        services.AddSingleton(_ => new CosmosClient(endpoint, key));

        // Repositories and Unit of Work
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}