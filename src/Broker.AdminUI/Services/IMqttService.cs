namespace Broker.AdminUI.Services;

/// <summary>
/// Service for MQTT operations (publish and subscribe).
/// </summary>
public interface IMqttService
{
    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    Task<bool> PublishAsync(string topic, string payload, int qos = 0, bool retain = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a topic filter.
    /// </summary>
    Task<bool> SubscribeAsync(string topicFilter, int qos = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from a topic filter.
    /// </summary>
    Task<bool> UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event that fires when a message is received on a subscribed topic.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// Event arguments for message received events.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int Qos { get; set; }
    public bool Retain { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

