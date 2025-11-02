using System.Text;

namespace Broker.Core.Packets;

/// <summary>
/// MQTT CONNECT packet - client request to connect to broker.
/// </summary>
public class MqttConnectPacket : MqttPacket
{
    public override MqttPacketType PacketType => MqttPacketType.CONNECT;

    /// <summary>
    /// Protocol name ("MQTT").
    /// </summary>
    public string ProtocolName { get; set; } = "MQTT";

    /// <summary>
    /// Protocol level (4 for MQTT v3.1.1).
    /// </summary>
    public byte ProtocolLevel { get; set; } = 4;

    /// <summary>
    /// Client identifier (required, 1-23 characters for MQTT v3.1.1).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Keep alive interval in seconds.
    /// </summary>
    public ushort KeepAlive { get; set; }

    /// <summary>
    /// Clean session flag - if true, broker will not store session state.
    /// </summary>
    public bool CleanSession { get; set; }

    /// <summary>
    /// Will message flag - indicates if will message is present.
    /// </summary>
    public bool WillFlag { get; set; }

    /// <summary>
    /// Will QoS level (0, 1, or 2).
    /// </summary>
    public QoSLevel WillQos { get; set; }

    /// <summary>
    /// Will retain flag - if true, will message should be retained.
    /// </summary>
    public bool WillRetain { get; set; }

    /// <summary>
    /// Username flag - indicates if username is present.
    /// </summary>
    public bool UsernameFlag { get; set; }

    /// <summary>
    /// Password flag - indicates if password is present.
    /// </summary>
    public bool PasswordFlag { get; set; }

    /// <summary>
    /// Will topic (only present if WillFlag is true).
    /// </summary>
    public string? WillTopic { get; set; }

    /// <summary>
    /// Will message payload (only present if WillFlag is true).
    /// </summary>
    public byte[]? WillMessage { get; set; }

    /// <summary>
    /// Username (only present if UsernameFlag is true).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password (only present if PasswordFlag is true).
    /// </summary>
    public byte[]? Password { get; set; }

    public override byte[] Serialize()
    {
        var writer = new MqttPacketWriter();

        // Write variable header
        writer.WriteString(ProtocolName);
        writer.WriteByte(ProtocolLevel);

        // Write connect flags
        byte connectFlags = 0;
        if (CleanSession) connectFlags |= 0x02;
        if (WillFlag)
        {
            connectFlags |= 0x04;
            connectFlags |= (byte)((byte)WillQos << 3);
            if (WillRetain) connectFlags |= 0x20;
        }
        if (PasswordFlag) connectFlags |= 0x40;
        if (UsernameFlag) connectFlags |= 0x80;

        writer.WriteByte(connectFlags);
        writer.WriteUInt16(KeepAlive);

        // Write payload
        writer.WriteString(ClientId);

        if (WillFlag && !string.IsNullOrEmpty(WillTopic))
        {
            writer.WriteString(WillTopic);
            if (WillMessage != null)
            {
                writer.WriteUInt16((ushort)WillMessage.Length);
                writer.WriteBytes(WillMessage);
            }
            else
            {
                writer.WriteUInt16(0);
            }
        }

        if (UsernameFlag && !string.IsNullOrEmpty(Username))
        {
            writer.WriteString(Username);
        }

        if (PasswordFlag && Password != null)
        {
            writer.WriteUInt16((ushort)Password.Length);
            writer.WriteBytes(Password);
        }

        byte[] payload = writer.ToArray();
        return BuildPacket(payload);
    }

    protected override int GetRemainingLength()
    {
        int length = 0;
        length += 2 + Encoding.UTF8.GetByteCount(ProtocolName); // Protocol name
        length += 1; // Protocol level
        length += 1; // Connect flags
        length += 2; // Keep alive

        // Payload
        length += 2 + Encoding.UTF8.GetByteCount(ClientId); // Client ID
        if (WillFlag && !string.IsNullOrEmpty(WillTopic))
        {
            length += 2 + Encoding.UTF8.GetByteCount(WillTopic); // Will topic
            length += 2 + (WillMessage?.Length ?? 0); // Will message
        }
        if (UsernameFlag && !string.IsNullOrEmpty(Username))
        {
            length += 2 + Encoding.UTF8.GetByteCount(Username); // Username
        }
        if (PasswordFlag && Password != null)
        {
            length += 2 + Password.Length; // Password
        }

        return length;
    }

    private byte[] BuildPacket(byte[] payload)
    {
        int remainingLength = payload.Length;
        byte[] variableLengthBytes = VariableLengthEncoder.Encode(remainingLength);

        // Fixed header: packet type + flags (always 0x00 for CONNECT)
        byte fixedHeader = (byte)((byte)PacketType << 4);
        byte[] fixedHeaderBytes = { fixedHeader };

        // Combine fixed header, variable length, and payload
        byte[] packet = new byte[fixedHeaderBytes.Length + variableLengthBytes.Length + payload.Length];
        Buffer.BlockCopy(fixedHeaderBytes, 0, packet, 0, fixedHeaderBytes.Length);
        Buffer.BlockCopy(variableLengthBytes, 0, packet, fixedHeaderBytes.Length, variableLengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, fixedHeaderBytes.Length + variableLengthBytes.Length, payload.Length);

        return packet;
    }

    public static MqttConnectPacket Deserialize(byte[] data)
    {
        var reader = new MqttPacketReader(data);
        
        // Skip fixed header (already validated)
        reader.ReadByte(); // Fixed header
        reader.ReadVariableLength(); // Variable length

        var packet = new MqttConnectPacket
        {
            ProtocolName = reader.ReadString(),
            ProtocolLevel = reader.ReadByte()
        };

        // Read connect flags
        byte connectFlags = reader.ReadByte();
        packet.CleanSession = (connectFlags & 0x02) != 0;
        packet.WillFlag = (connectFlags & 0x04) != 0;
        packet.WillQos = (QoSLevel)((connectFlags >> 3) & 0x03);
        packet.WillRetain = (connectFlags & 0x20) != 0;
        packet.PasswordFlag = (connectFlags & 0x40) != 0;
        packet.UsernameFlag = (connectFlags & 0x80) != 0;

        packet.KeepAlive = reader.ReadUInt16();

        // Read payload
        packet.ClientId = reader.ReadString();

        if (packet.WillFlag)
        {
            packet.WillTopic = reader.ReadString();
            ushort willMessageLength = reader.ReadUInt16();
            if (willMessageLength > 0)
            {
                packet.WillMessage = reader.ReadBytes(willMessageLength);
            }
        }

        if (packet.UsernameFlag)
        {
            packet.Username = reader.ReadString();
        }

        if (packet.PasswordFlag)
        {
            ushort passwordLength = reader.ReadUInt16();
            if (passwordLength > 0)
            {
                packet.Password = reader.ReadBytes(passwordLength);
            }
        }

        return packet;
    }
}

