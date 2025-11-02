namespace Broker.Core.Storage.Models;

/// <summary>
/// Represents a retained message stored for a topic.
/// </summary>
public class RetainedMessage
{
    /// <summary>
    /// Topic name (used as key).
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Message payload.
    /// </summary>
    public byte[]? Payload { get; set; }

    /// <summary>
    /// QoS level.
    /// </summary>
    public QoSLevel Qos { get; set; }

    /// <summary>
    /// Timestamp when the message was retained.
    /// </summary>
    public DateTime RetainedAt { get; set; }
}

