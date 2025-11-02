namespace Broker.Core.Routing;

/// <summary>
/// Interface for broker metrics collection and reporting.
/// </summary>
public interface IBrokerMetricsService
{
    /// <summary>
    /// Gets the number of currently connected clients.
    /// </summary>
    int ConnectedClients { get; }

    /// <summary>
    /// Gets the total number of messages published since startup.
    /// </summary>
    long TotalMessagesPublished { get; }

    /// <summary>
    /// Gets the total number of messages received since startup.
    /// </summary>
    long TotalMessagesReceived { get; }

    /// <summary>
    /// Gets the messages per second (calculated over the last minute).
    /// </summary>
    double MessagesPerSecond { get; }

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    int ActiveSubscriptions { get; }

    /// <summary>
    /// Gets the number of retained messages.
    /// </summary>
    int RetainedMessages { get; }

    /// <summary>
    /// Gets the number of pending messages (QoS 1/2).
    /// </summary>
    int PendingMessages { get; }

    /// <summary>
    /// Records a message being published.
    /// </summary>
    void RecordMessagePublished();

    /// <summary>
    /// Records a message being received.
    /// </summary>
    void RecordMessageReceived();

    /// <summary>
    /// Updates the connected clients count.
    /// </summary>
    /// <param name="count">The new count.</param>
    void UpdateConnectedClients(int count);

    /// <summary>
    /// Updates the active subscriptions count.
    /// </summary>
    /// <param name="count">The new count.</param>
    void UpdateActiveSubscriptions(int count);

    /// <summary>
    /// Updates the retained messages count.
    /// </summary>
    /// <param name="count">The new count.</param>
    void UpdateRetainedMessages(int count);

    /// <summary>
    /// Updates the pending messages count.
    /// </summary>
    /// <param name="count">The new count.</param>
    void UpdatePendingMessages(int count);
}

