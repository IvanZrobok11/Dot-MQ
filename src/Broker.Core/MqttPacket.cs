namespace Broker.Core;

/// <summary>
/// Base class for all MQTT packets.
/// </summary>
public abstract class MqttPacket
{
    /// <summary>
    /// Gets the packet type.
    /// </summary>
    public abstract MqttPacketType PacketType { get; }

    /// <summary>
    /// Gets or sets the fixed header flags (bits 4-7 of the first byte).
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>
    /// Serializes the packet to a byte array.
    /// </summary>
    /// <returns>The serialized packet bytes.</returns>
    public abstract byte[] Serialize();

    /// <summary>
    /// Gets the remaining length (payload length) of the packet.
    /// </summary>
    /// <returns>The remaining length in bytes.</returns>
    protected abstract int GetRemainingLength();
}

