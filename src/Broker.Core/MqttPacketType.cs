namespace Broker.Core;

/// <summary>
/// MQTT packet type identifiers as defined in MQTT v3.1.1 specification.
/// Packed into the first 4 bits of the fixed header.
/// </summary>
public enum MqttPacketType : byte
{
    /// <summary>
    /// Client request to connect to broker
    /// </summary>
    CONNECT = 1,

    /// <summary>
    /// Connection acknowledgment from broker
    /// </summary>
    CONNACK = 2,

    /// <summary>
    /// Publish message
    /// </summary>
    PUBLISH = 3,

    /// <summary>
    /// Publish acknowledgment (QoS 1)
    /// </summary>
    PUBACK = 4,

    /// <summary>
    /// Publish received (QoS 2, part 1)
    /// </summary>
    PUBREC = 5,

    /// <summary>
    /// Publish release (QoS 2, part 2)
    /// </summary>
    PUBREL = 6,

    /// <summary>
    /// Publish complete (QoS 2, part 3)
    /// </summary>
    PUBCOMP = 7,

    /// <summary>
    /// Client subscribe request
    /// </summary>
    SUBSCRIBE = 8,

    /// <summary>
    /// Subscribe acknowledgment
    /// </summary>
    SUBACK = 9,

    /// <summary>
    /// Client unsubscribe request
    /// </summary>
    UNSUBSCRIBE = 10,

    /// <summary>
    /// Unsubscribe acknowledgment
    /// </summary>
    UNSUBACK = 11,

    /// <summary>
    /// PING request (keep-alive)
    /// </summary>
    PINGREQ = 12,

    /// <summary>
    /// PING response
    /// </summary>
    PINGRESP = 13,

    /// <summary>
    /// Client disconnect notification
    /// </summary>
    DISCONNECT = 14
}

