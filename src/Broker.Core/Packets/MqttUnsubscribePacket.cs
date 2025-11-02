using System.Text;

namespace Broker.Core.Packets;

/// <summary>
/// MQTT UNSUBSCRIBE packet - unsubscribe from topics.
/// </summary>
public class MqttUnsubscribePacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.UNSUBSCRIBE;

    /// <summary>
    /// Packet identifier (required for UNSUBSCRIBE).
    /// </summary>
    public ushort PacketId { get; set; }

    /// <summary>
    /// List of topic filters to unsubscribe from.
    /// </summary>
    public List<string> TopicFilters { get; set; } = new();

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header
        writer.WriteUInt16(PacketId);

        // Payload
        foreach (string topicFilter in TopicFilters)
        {
            writer.WriteString(topicFilter);
        }

        byte[] payload = writer.ToArray();

        // Fixed header: packet type + flags (UNSUBSCRIBE requires flags = 0x02)
        Flags = 0x02;
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        int length = 2; // Packet ID

        foreach (string topicFilter in TopicFilters)
        {
            length += 2 + Encoding.UTF8.GetByteCount(topicFilter); // Topic filter
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

    public static MqttUnsubscribePacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Read fixed header
        byte fixedHeader = reader.ReadByte();
        reader.ReadVariableLength(); // Variable length

        var packet = new MqttUnsubscribePacket
        {
            Flags = (byte)(fixedHeader & 0x0F),
            PacketId = reader.ReadUInt16()
        };

        // Read topic filters
        while (reader.HasMoreData)
        {
            packet.TopicFilters.Add(reader.ReadString());
        }

        return packet;
    }
}

