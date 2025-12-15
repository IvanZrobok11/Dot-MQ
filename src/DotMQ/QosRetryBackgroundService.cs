using Broker.Core;
using Broker.Core.Qos;
using Broker.Core.Storage.Models;

namespace DotMQ;

/// <summary>
/// Background service that periodically retries unacknowledged QoS 1 messages.
/// </summary>
public class QosRetryBackgroundService(IPendingStore pendingStore, ILogger<QosRetryBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds
    private readonly TimeSpan _retryTimeout = TimeSpan.FromMinutes(1); // Retry messages older than 1 minute

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger?.LogInformation("QoS 1 retry service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // await _retryService.RetryUnacknowledgedMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in QoS 1 retry service");
            }

            try
            {
                await Task.Delay(_retryInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger?.LogInformation("QoS 1 retry service stopped");
    }

    /// <summary>
    /// Retries unacknowledged QoS 1 messages. Should be called periodically.
    /// </summary>
    public async Task RetryUnacknowledgedMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allPendingMessages = await pendingStore.GetAllPendingMessagesAsync(cancellationToken);
            var qos1Messages = allPendingMessages
                .Where(m => m.Qos == QoSLevel.AtLeastOnce)
                .Where(m => ShouldRetry(m))
                .GroupBy(m => m.ClientId)
                .ToList();

            if (qos1Messages.Count == 0)
            {
                return;
            }

            logger.LogDebug("Retrying {Count} unacknowledged QoS 1 messages", qos1Messages.Sum(g => g.Count()));

            foreach (var clientGroup in qos1Messages)
            {
                // Deliver pending messages for this client
                //await _qos1Handler.DeliverPendingMessagesAsync(clientGroup.Key, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrying unacknowledged messages");
        }
    }

    private bool ShouldRetry(PendingMessage message)
    {
        // Retry if message is older than retry timeout
        var age = DateTime.UtcNow - message.QueuedAt;
        return age >= _retryTimeout;
    }
}

