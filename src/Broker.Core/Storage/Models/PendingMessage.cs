namespace Broker.Core.Storage.Models;

/// <summary>
/// Represents a pending message waiting for acknowledgment (QoS 1 or 2).
/// </summary>
public class PendingMessage
{
    /// <summary>
    /// Unique identifier for this pending message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Client identifier that should receive this message.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Packet identifier (for QoS 1/2).
    /// </summary>
    public ushort PacketId { get; set; }

    /// <summary>
    /// Topic name.
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
    /// Retain flag.
    /// </summary>
    public bool Retain { get; set; }

    /// <summary>
    /// Timestamp when the message was queued.
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Timestamp of the last retry attempt.
    /// </summary>
    public DateTime? LastRetryAt { get; set; }
}

