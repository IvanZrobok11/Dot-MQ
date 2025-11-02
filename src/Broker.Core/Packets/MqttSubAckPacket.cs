namespace Broker.Core.Packets;

/// <summary>
/// MQTT SUBACK packet - acknowledgment of subscription request.
/// </summary>
public class MqttSubAckPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.SUBACK;

    /// <summary>
    /// Packet identifier (matches SUBSCRIBE packet).
    /// </summary>
    public ushort PacketId { get; set; }

    /// <summary>
    /// List of granted QoS levels (one per subscription in SUBSCRIBE).
    /// </summary>
    public List<QoSLevel> GrantedQosLevels { get; set; } = new();

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header
        writer.WriteUInt16(PacketId);

        // Payload - return codes
        foreach (var qos in GrantedQosLevels)
        {
            writer.WriteByte((byte)qos);
        }

        byte[] payload = writer.ToArray();
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        return 2 + GrantedQosLevels.Count; // Packet ID + return codes
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        // Fixed header: packet type + flags (always 0x00 for SUBACK)
        byte fixedHeader = (byte)((byte)PacketType << 4);
        byte[] fixedHeaderBytes = { fixedHeader };

        // Combine fixed header, variable length, and payload
        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttSubAckPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Skip fixed header
        reader.ReadByte();
        reader.ReadVariableLength();

        var packet = new MqttSubAckPacket
        {
            PacketId = reader.ReadUInt16()
        };

        // Read granted QoS levels
        while (reader.HasMoreData)
        {
            byte qos = reader.ReadByte();
            packet.GrantedQosLevels.Add((QoSLevel)qos);
        }

        return packet;
    }
}

