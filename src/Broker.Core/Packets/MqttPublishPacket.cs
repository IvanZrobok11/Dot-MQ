using System.Text;

namespace Broker.Core.Packets;

/// <summary>
/// MQTT PUBLISH packet - publish a message to a topic.
/// </summary>
public class MqttPublishPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.PUBLISH;

    /// <summary>
    /// Topic name to publish to.
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Packet identifier (required for QoS > 0).
    /// </summary>
    public ushort? PacketId { get; set; }

    /// <summary>
    /// Quality of Service level.
    /// </summary>
    public QoSLevel QoS { get; set; }

    /// <summary>
    /// Retain flag - if true, broker should retain this message.
    /// </summary>
    public bool Retain { get; set; }

    /// <summary>
    /// Duplicate flag - indicates if this is a duplicate message (QoS > 0).
    /// </summary>
    public bool Duplicate { get; set; }

    /// <summary>
    /// Message payload.
    /// </summary>
    public byte[]? Payload { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header
        writer.WriteString(TopicName);

        if (QoS > QoSLevel.AtMostOnce)
        {
            if (!PacketId.HasValue)
            {
                throw new InvalidOperationException("PacketId is required for QoS > 0");
            }
            writer.WriteUInt16(PacketId.Value);
        }

        // Payload
        if (Payload != null && Payload.Length > 0)
        {
            writer.WriteBytes(Payload);
        }

        byte[] payload = writer.ToArray();

        // Build fixed header with flags
        byte flags = 0;
        if (Duplicate) flags |= 0x08;
        flags |= (byte)((byte)QoS << 1);
        if (Retain) flags |= 0x01;
        Flags = flags;

        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        int length = 0;
        length += 2 + Encoding.UTF8.GetByteCount(TopicName); // Topic name
        if (QoS > QoSLevel.AtMostOnce)
        {
            length += 2; // Packet ID
        }
        if (Payload != null)
        {
            length += Payload.Length; // Payload
        }
        return length;
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        // Fixed header: packet type + flags
        byte fixedHeader = (byte)((byte)PacketType << 4 | Flags);
        byte[] fixedHeaderBytes = { fixedHeader };

        // Combine fixed header, variable length, and payload
        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttPublishPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Read fixed header
        byte fixedHeader = reader.ReadByte();
        byte flags = (byte)(fixedHeader & 0x0F);
        reader.ReadVariableLength(); // Variable length

        var packet = new MqttPublishPacket
        {
            Flags = flags,
            Duplicate = (flags & 0x08) != 0,
            QoS = (QoSLevel)((flags >> 1) & 0x03),
            Retain = (flags & 0x01) != 0
        };

        // Variable header
        packet.TopicName = reader.ReadString();

        if (packet.QoS > QoSLevel.AtMostOnce)
        {
            packet.PacketId = reader.ReadUInt16();
        }

        // Payload
        if (reader.HasMoreData)
        {
            int remaining = (int)(reader.Length - reader.Position);
            if (remaining > 0)
            {
                packet.Payload = reader.ReadBytes(remaining);
            }
        }

        return packet;
    }
}

