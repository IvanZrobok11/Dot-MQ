namespace Broker.Core.Packets;

/// <summary>
/// MQTT PINGRESP packet - response to PINGREQ.
/// </summary>
public class MqttPingRespPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.PINGRESP;

    public override byte[] Serialize()
    {
        // PINGRESP has no variable header or payload
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(0);
        byte fixedHeader = (byte)((byte)PacketType << 4);
        
        byte[] packet = new byte[1 + variableLengthBytes.Length];
        packet[0] = fixedHeader;
        Buffer.BlockCopy(variableLengthBytes, 0, packet, 1, variableLengthBytes.Length);

        return packet;
    }

    protected override int GetRemainingLength()
    {
        return 0; // No variable header or payload
    }
}

