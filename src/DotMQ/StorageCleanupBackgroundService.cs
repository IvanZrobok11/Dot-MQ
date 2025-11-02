using Broker.Storage;
using Microsoft.Extensions.Options;

namespace DotMQ;

/// <summary>
/// Background service that periodically cleans up old storage data.
/// </summary>
public class StorageCleanupBackgroundService : BackgroundService
{
    private readonly StorageOptions _options;
    private readonly ILogger<StorageCleanupBackgroundService>? _logger;

    public StorageCleanupBackgroundService(
        IOptions<StorageOptions> options,
        ILogger<StorageCleanupBackgroundService>? logger = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("Storage cleanup service started (interval: {Interval})", _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //await PerformCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in storage cleanup service");
            }

            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger?.LogInformation("Storage cleanup service stopped");
    }

    //private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    //{
    //    _logger?.LogDebug("Starting storage cleanup...");

    //    try
    //    {
    //        // Cleanup old sessions
    //        var sessionsDeleted = await _storageProvider.CleanupOldSessionsAsync(
    //            _options.SessionRetentionPeriod, cancellationToken);

    //        // Cleanup old pending messages
    //        var pendingMessagesDeleted = await _storageProvider.CleanupOldPendingMessagesAsync(
    //            _options.PendingMessageRetentionPeriod, cancellationToken);

    //        // Cleanup old QoS 2 states
    //        var qos2StatesDeleted = await _storageProvider.CleanupOldQos2StatesAsync(
    //            _options.Qos2StateRetentionPeriod, cancellationToken);

    //        if (sessionsDeleted > 0 || pendingMessagesDeleted > 0 || qos2StatesDeleted > 0)
    //        {
    //            _logger?.LogInformation(
    //                "Storage cleanup completed: {Sessions} sessions, {PendingMessages} pending messages, {Qos2States} QoS 2 states deleted",
    //                sessionsDeleted, pendingMessagesDeleted, qos2StatesDeleted);
    //        }
    //        else
    //        {
    //            _logger?.LogDebug("Storage cleanup completed: no old data found");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger?.LogError(ex, "Error during storage cleanup");
    //    }
    //}
}

