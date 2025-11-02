using System.Text;

namespace Broker.Core;

/// <summary>
/// Writes MQTT packets to a binary stream.
/// </summary>
public class MqttPacketWriter
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    public MqttPacketWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
    }

    /// <summary>
    /// Writes a UTF-8 encoded string (MSB length-prefixed).
    /// </summary>
    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUInt16(0);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt16((ushort)bytes.Length);
        _writer.Write(bytes);
    }

    /// <summary>
    /// Writes a 16-bit unsigned integer (big-endian).
    /// </summary>
    public void WriteUInt16(ushort value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _writer.Write(bytes);
    }

    /// <summary>
    /// Writes a byte.
    /// </summary>
    public void WriteByte(byte value)
    {
        _writer.Write(value);
    }

    /// <summary>
    /// Writes a byte array.
    /// </summary>
    public void WriteBytes(byte[] value)
    {
        _writer.Write(value);
    }

    /// <summary>
    /// Writes a variable-length integer.
    /// </summary>
    public void WriteVariableLength(int value)
    {
        byte[] encoded = VariableLengthEncoder.Encode(value);
        _writer.Write(encoded);
    }

    /// <summary>
    /// Gets the written data as a byte array.
    /// </summary>
    public byte[] ToArray()
    {
        return _stream.ToArray();
    }

    /// <summary>
    /// Gets the current length of the written data.
    /// </summary>
    public int Length => (int)_stream.Length;
}

