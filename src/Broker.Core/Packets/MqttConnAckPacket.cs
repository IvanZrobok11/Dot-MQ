namespace Broker.Core.Packets;

/// <summary>
/// MQTT CONNACK packet - broker acknowledgment of connection request.
/// </summary>
public class MqttConnAckPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.CONNACK;

    /// <summary>
    /// Session present flag - indicates if broker has stored session state.
    /// </summary>
    public bool SessionPresent { get; set; }

    /// <summary>
    /// Connection return code.
    /// </summary>
    public MqttConnectReturnCode ReturnCode { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Variable header (2 bytes)
        byte connectAckFlags = 0;
        if (SessionPresent) connectAckFlags |= 0x01;
        writer.WriteByte(connectAckFlags);
        writer.WriteByte((byte)ReturnCode);

        byte[] payload = writer.ToArray();
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        return 2; // Fixed 2 bytes for CONNACK
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        // Fixed header: packet type + flags (always 0x00 for CONNACK)
        byte fixedHeader = (byte)((byte)PacketType << 4);
        byte[] fixedHeaderBytes = { fixedHeader };

        // Combine fixed header, variable length, and payload
        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttConnAckPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Skip fixed header
        reader.ReadByte();
        reader.ReadVariableLength();

        byte flags = reader.ReadByte();
        byte returnCode = reader.ReadByte();

        return new MqttConnAckPacket
        {
            SessionPresent = (flags & 0x01) != 0,
            ReturnCode = (MqttConnectReturnCode)returnCode
        };
    }
}

/// <summary>
/// MQTT connection return codes.
/// </summary>
public enum MqttConnectReturnCode : byte
{
    /// <summary>
    /// Connection accepted
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// Connection refused: unacceptable protocol version
    /// </summary>
    UnacceptableProtocolVersion = 1,

    /// <summary>
    /// Connection refused: identifier rejected
    /// </summary>
    IdentifierRejected = 2,

    /// <summary>
    /// Connection refused: server unavailable
    /// </summary>
    ServerUnavailable = 3,

    /// <summary>
    /// Connection refused: bad user name or password
    /// </summary>
    BadUserNameOrPassword = 4,

    /// <summary>
    /// Connection refused: not authorized
    /// </summary>
    NotAuthorized = 5
}

