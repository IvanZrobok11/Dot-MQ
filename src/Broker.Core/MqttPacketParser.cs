using Broker.Core.Packets;

namespace Broker.Core;

/// <summary>
/// Parser for MQTT packets from byte arrays.
/// </summary>
public static class MqttPacketParser
{
    /// <summary>
    /// Parses a packet from a byte array.
    /// </summary>
    /// <param name="data">The packet bytes.</param>
    /// <returns>The parsed packet.</returns>
    /// <exception cref="ArgumentException">Thrown if the packet is malformed.</exception>
    public static MqttPacket Parse(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Packet data cannot be null or empty", nameof(data));
        }

        if (data.Length < 2)
        {
            throw new ArgumentException("Packet is too short (minimum 2 bytes required)", nameof(data));
        }

        // Read fixed header
        byte fixedHeader = data[0];
        MqttPacketType packetType = (MqttPacketType)((fixedHeader >> 4) & 0x0F);
        byte flags = (byte)(fixedHeader & 0x0F);

        // Parse based on packet type
        return packetType switch
        {
            MqttPacketType.CONNECT => MqttConnectPacket.Deserialize(data),
            MqttPacketType.CONNACK => MqttConnAckPacket.Deserialize(data),
            MqttPacketType.PUBLISH => MqttPublishPacket.Deserialize(data),
            MqttPacketType.PUBACK => MqttPubAckPacket.Deserialize(data),
            MqttPacketType.PUBREC => MqttPubRecPacket.Deserialize(data),
            MqttPacketType.PUBREL => MqttPubRelPacket.Deserialize(data),
            MqttPacketType.PUBCOMP => MqttPubCompPacket.Deserialize(data),
            MqttPacketType.SUBSCRIBE => MqttSubscribePacket.Deserialize(data),
            MqttPacketType.SUBACK => MqttSubAckPacket.Deserialize(data),
            MqttPacketType.UNSUBSCRIBE => MqttUnsubscribePacket.Deserialize(data),
            MqttPacketType.UNSUBACK => MqttUnsubAckPacket.Deserialize(data),
            MqttPacketType.PINGREQ => new MqttPingReqPacket(),
            MqttPacketType.PINGRESP => new MqttPingRespPacket(),
            MqttPacketType.DISCONNECT => new MqttDisconnectPacket(),
            _ => throw new ArgumentException($"Unknown packet type: {packetType}", nameof(data))
        };
    }
}

