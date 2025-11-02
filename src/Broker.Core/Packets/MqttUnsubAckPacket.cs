namespace Broker.Core.Packets;

/// <summary>
/// MQTT UNSUBACK packet - acknowledgment of unsubscribe request.
/// </summary>
public class MqttUnsubAckPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.UNSUBACK;

    /// <summary>
    /// Packet identifier (matches UNSUBSCRIBE packet).
    /// </summary>
    public ushort PacketId { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header (only packet ID)
        writer.WriteUInt16(PacketId);

        byte[] payload = writer.ToArray();
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        return 2; // Packet ID only
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        // Fixed header: packet type + flags (always 0x00 for UNSUBACK)
        byte fixedHeader = (byte)((byte)PacketType << 4);
        byte[] fixedHeaderBytes = { fixedHeader };

        // Combine fixed header, variable length, and payload
        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttUnsubAckPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Skip fixed header
        reader.ReadByte();
        reader.ReadVariableLength();

        return new MqttUnsubAckPacket
        {
            PacketId = reader.ReadUInt16()
        };
    }
}

