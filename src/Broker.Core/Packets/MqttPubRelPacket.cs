namespace Broker.Core.Packets;

/// <summary>
/// MQTT PUBREL packet - QoS 2 publish release (step 2).
/// </summary>
public class MqttPubRelPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.PUBREL;

    public ushort PacketId { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();
        writer.WriteUInt16(PacketId);

        byte[] payload = writer.ToArray();
        Flags = 0x02; // PUBREL requires flags = 0x02
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        return 2;
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        byte fixedHeader = (byte)((byte)PacketType << 4 | Flags);
        byte[] fixedHeaderBytes = { fixedHeader };

        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttPubRelPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        byte fixedHeader = reader.ReadByte();
        reader.ReadVariableLength();

        return new MqttPubRelPacket
        {
            Flags = (byte)(fixedHeader & 0x0F),
            PacketId = reader.ReadUInt16()
        };
    }
}

