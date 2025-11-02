using System.Text;

namespace Broker.Core;

/// <summary>
/// Reads MQTT packets from a binary stream.
/// </summary>
public class MqttPacketReader
{
    private readonly BinaryReader _reader;

    public MqttPacketReader(Stream stream)
    {
        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
    }

    public MqttPacketReader(byte[] data)
    {
        _reader = new BinaryReader(new MemoryStream(data));
    }

    /// <summary>
    /// Reads a UTF-8 encoded string (MSB length-prefixed).
    /// </summary>
    public string ReadString()
    {
        ushort length = ReadUInt16();
        if (length == 0)
        {
            return string.Empty;
        }

        byte[] bytes = _reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer (big-endian).
    /// </summary>
    public ushort ReadUInt16()
    {
        byte[] bytes = _reader.ReadBytes(2);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt16(bytes, 0);
    }

    /// <summary>
    /// Reads a byte.
    /// </summary>
    public byte ReadByte()
    {
        return _reader.ReadByte();
    }

    /// <summary>
    /// Reads a specified number of bytes.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        return _reader.ReadBytes(count);
    }

    /// <summary>
    /// Reads a variable-length integer.
    /// </summary>
    public int ReadVariableLength()
    {
        return VariableLengthEncoder.Decode(_reader);
    }

    /// <summary>
    /// Gets the position in the stream.
    /// </summary>
    public long Position => _reader.BaseStream.Position;

    /// <summary>
    /// Gets the length of the stream.
    /// </summary>
    public long Length => _reader.BaseStream.Length;

    /// <summary>
    /// Checks if there are more bytes to read.
    /// </summary>
    public bool HasMoreData => Position < Length;
}

