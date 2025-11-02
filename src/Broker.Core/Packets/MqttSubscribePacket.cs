using System.Text;

namespace Broker.Core.Packets;

/// <summary>
/// MQTT SUBSCRIBE packet - subscribe to one or more topics.
/// </summary>
public class MqttSubscribePacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.SUBSCRIBE;

    /// <summary>
    /// Packet identifier (required for SUBSCRIBE).
    /// </summary>
    public ushort PacketId { get; set; }

    /// <summary>
    /// List of topic filters and their requested QoS levels.
    /// </summary>
    public List<(string TopicFilter, QoSLevel RequestedQos)> Subscriptions { get; set; } = new();

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header
        writer.WriteUInt16(PacketId);

        // Payload
        foreach (var (topicFilter, requestedQos) in Subscriptions)
        {
            writer.WriteString(topicFilter);
            writer.WriteByte((byte)requestedQos);
        }

        byte[] payload = writer.ToArray();

        // Fixed header: packet type + flags (SUBSCRIBE requires flags = 0x02)
        Flags = 0x02;
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        int length = 2; // Packet ID

        foreach (var (topicFilter, _) in Subscriptions)
        {
            length += 2 + Encoding.UTF8.GetByteCount(topicFilter); // Topic filter
            length += 1; // Requested QoS
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

    public static MqttSubscribePacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Read fixed header
        byte fixedHeader = reader.ReadByte();
        reader.ReadVariableLength(); // Variable length

        var packet = new MqttSubscribePacket
        {
            Flags = (byte)(fixedHeader & 0x0F),
            PacketId = reader.ReadUInt16()
        };

        // Read subscriptions
        while (reader.HasMoreData)
        {
            string topicFilter = reader.ReadString();
            byte requestedQos = reader.ReadByte();
            packet.Subscriptions.Add((topicFilter, (QoSLevel)requestedQos));
        }

        return packet;
    }
}

