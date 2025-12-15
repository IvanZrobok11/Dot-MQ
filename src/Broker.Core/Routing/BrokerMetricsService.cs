using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Broker.Core.Routing;

/// <summary>
/// Service for collecting and reporting broker metrics.
/// </summary>
public class BrokerMetricsService(ILogger<BrokerMetricsService> logger) : IBrokerMetricsService
{
    private readonly ConcurrentQueue<DateTime> _messageTimestamps = new();
    private readonly TimeSpan _metricsWindow = TimeSpan.FromMinutes(1);

    private int _connectedClients;
    private long _totalMessagesPublished;
    private long _totalMessagesReceived;
    private int _activeSubscriptions;
    private int _retainedMessages;
    private int _pendingMessages;

    public int ConnectedClients => _connectedClients;
    public long TotalMessagesPublished => _totalMessagesPublished;
    public long TotalMessagesReceived => _totalMessagesReceived;
    public double MessagesPerSecond
    {
        get
        {
            var cutoff = DateTime.UtcNow - _metricsWindow;

            // Remove old timestamps
            while (_messageTimestamps.TryPeek(out var timestamp) && timestamp < cutoff)
            {
                _messageTimestamps.TryDequeue(out _);
            }

            var messageCount = _messageTimestamps.Count;
            return messageCount / _metricsWindow.TotalSeconds;
        }
    }
    public int ActiveSubscriptions => _activeSubscriptions;
    public int RetainedMessages => _retainedMessages;
    public int PendingMessages => _pendingMessages;

    public void RecordMessagePublished()
    {
        Interlocked.Increment(ref _totalMessagesPublished);
        _messageTimestamps.Enqueue(DateTime.UtcNow);
        logger.LogTrace("Recorded message published. Total: {Total}", _totalMessagesPublished);
    }

    public void RecordMessageReceived()
    {
        Interlocked.Increment(ref _totalMessagesReceived);
        _messageTimestamps.Enqueue(DateTime.UtcNow);
        logger.LogTrace("Recorded message received. Total: {Total}", _totalMessagesReceived);
    }

    public void UpdateConnectedClients(int count)
    {
        Interlocked.Exchange(ref _connectedClients, count);
        logger.LogDebug("Updated connected clients count: {Count}", count);
    }

    public void UpdateActiveSubscriptions(int count)
    {
        Interlocked.Exchange(ref _activeSubscriptions, count);
        logger.LogDebug("Updated active subscriptions count: {Count}", count);
    }

    public void UpdateRetainedMessages(int count)
    {
        Interlocked.Exchange(ref _retainedMessages, count);
        logger.LogDebug("Updated retained messages count: {Count}", count);
    }

    public void UpdatePendingMessages(int count)
    {
        Interlocked.Exchange(ref _pendingMessages, count);
        logger.LogDebug("Updated pending messages count: {Count}", count);
    }
}

