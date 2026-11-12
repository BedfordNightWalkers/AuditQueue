
using System.Text.Json;
using Audit.Application.Abstractions;
using Audit.Contracts;
using Audit.Infrastructure.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Processor;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    private const int BatchSize = 10;

    private static readonly TimeSpan PollInterval =
        TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Publisher Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);

                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "An unexpected error occurred in the Outbox Publisher.");

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        logger.LogInformation("Outbox Publisher Worker stopped.");
    }

    private async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        // Create a scope because the repository and DbContext are Scoped.
        using var scope = scopeFactory.CreateScope();

        var outboxRepository = scope.ServiceProvider
            .GetRequiredService<IOutboxRepository>();

        var queueSender = scope.ServiceProvider
            .GetRequiredService<IAuditQueueSender>();

        var messages = await outboxRepository.GetPendingAsync(
            BatchSize,
            cancellationToken);

        if (messages.Count == 0)
        {
            logger.LogDebug("No pending Outbox messages found.");
            return;
        }

        logger.LogInformation(
            "Found {Count} pending Outbox messages.",
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Convert the stored JSON back into an AuditEvent.
                var auditEvent =
                    JsonSerializer.Deserialize<AuditEvent>(
                        message.PayloadJson);

                if (auditEvent is null)
                {
                    throw new InvalidOperationException(
                        $"Could not deserialize Outbox message {message.Id}.");
                }

                // Publish the event to Azure Queue Storage.
                await queueSender.SendAsync(
                    auditEvent,
                    cancellationToken);

                // Only mark as published after a successful send.
                await outboxRepository.MarkPublishedAsync(
                    message.Id,
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                logger.LogInformation(
                    "Published audit event {EventId} ({EventType}).",
                    message.EventId,
                    message.EventType);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to publish Outbox message {EventId}.",
                    message.EventId);

                try
                {
                    await outboxRepository.RecordFailureAsync(
                        message.Id,
                        ex.Message,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception recordException)
                {
                    logger.LogError(
                        recordException,
                        "Failed to record publication failure for Outbox message {EventId}.",
                        message.EventId);
                }
            }
        }
    }
}
