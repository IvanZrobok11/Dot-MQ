namespace Broker.Core.Packets;

/// <summary>
/// MQTT PUBCOMP packet - QoS 2 publish complete (step 3).
/// </summary>
public class MqttPubCompPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.PUBCOMP;

    public ushort PacketId { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();
        writer.WriteUInt16(PacketId);

        byte[] payload = writer.ToArray();
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

        byte fixedHeader = (byte)((byte)PacketType << 4);
        byte[] fixedHeaderBytes = { fixedHeader };

        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttPubCompPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        reader.ReadByte();
        reader.ReadVariableLength();

        return new MqttPubCompPacket
        {
            PacketId = reader.ReadUInt16()
        };
    }
}

