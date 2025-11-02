namespace Broker.Core;

/// <summary>
/// Quality of Service levels for MQTT message delivery.
/// </summary>
public enum QoSLevel : byte
{
    /// <summary>
    /// At most once delivery (fire-and-forget)
    /// </summary>
    AtMostOnce = 0,

    /// <summary>
    /// At least once delivery (acknowledged)
    /// </summary>
    AtLeastOnce = 1,

    /// <summary>
    /// Exactly once delivery (two-phase handshake)
    /// </summary>
    ExactlyOnce = 2
}

