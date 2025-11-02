namespace Broker.Core.Storage.Models;

/// <summary>
/// Represents a client session stored in the broker.
/// </summary>
public class MqttSession
{
    /// <summary>
    /// Unique client identifier.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Whether the session should be cleaned on disconnect.
    /// </summary>
    public bool CleanSession { get; set; }

    /// <summary>
    /// Client subscriptions (topic filter -> QoS level).
    /// </summary>
    public Dictionary<string, QoSLevel> Subscriptions { get; set; } = new();

    /// <summary>
    /// Will message to publish if client disconnects unexpectedly.
    /// </summary>
    public WillMessage? WillMessage { get; set; }

    /// <summary>
    /// Timestamp when the session was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}




/// <summary>
/// Represents a will message for a client session.
/// </summary>
/// <param name="Topic"> Topic to publish the will message to. </param>
/// <param name="Payload"> Message payload. </param>
/// <param name="QoS"> QoS level for the will message. </param>
/// <param name="Retain"> Retain flag for the will message. </param>
public record WillMessage(string Topic, byte[]? Payload, QoSLevel QoS, bool Retain);
