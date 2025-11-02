namespace Broker.Core;

/// <summary>
/// Helper class for encoding and decoding MQTT variable-length integers.
/// MQTT uses a variable-length encoding scheme where bytes with MSB set indicate continuation.
/// </summary>
public static class VariableLengthEncoder
{
    private const byte ContinuationBit = 0x80;
    private const byte ValueMask = 0x7F;
    private const int MaxLength = 4; // Maximum 4 bytes for variable length encoding
    private const int MaxValue = 268_435_455; // Maximum value (2^28 - 1)

    /// <summary>
    /// Encodes a value into a variable-length byte array.
    /// </summary>
    /// <param name="value">The value to encode (0 to 268,435,455).</param>
    /// <returns>The encoded bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value exceeds maximum.</exception>
    public static byte[] Encode(int value)
    {
        if (value < 0 || value > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value must be between 0 and {MaxValue}");
        }

        var buffer = new List<byte>();
        int x = value;

        do
        {
            byte encodedByte = (byte)(x % 128);
            x /= 128;
            if (x > 0)
            {
                encodedByte |= ContinuationBit;
            }
            buffer.Add(encodedByte);
        } while (x > 0);

        return buffer.ToArray();
    }

    /// <summary>
    /// Decodes a variable-length integer from a stream.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the variable-length integer.</param>
    /// <returns>The decoded integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the encoding is invalid or exceeds maximum length.</exception>
    public static int Decode(BinaryReader reader)
    {
        int multiplier = 1;
        int value = 0;
        int position = 0;
        byte encodedByte;

        do
        {
            if (position >= MaxLength)
            {
                throw new InvalidOperationException("Variable-length encoding exceeds maximum length of 4 bytes");
            }

            encodedByte = reader.ReadByte();
            value += (encodedByte & ValueMask) * multiplier;
            multiplier *= 128;
            position++;
        } while ((encodedByte & ContinuationBit) != 0);

        if (value > MaxValue)
        {
            throw new InvalidOperationException($"Decoded value {value} exceeds maximum {MaxValue}");
        }

        return value;
    }

    /// <summary>
    /// Calculates the number of bytes required to encode a value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The number of bytes required.</returns>
    public static int GetEncodedLength(int value)
    {
        if (value < 0 || value > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == 0)
        {
            return 1;
        }

        int length = 0;
        int x = value;
        do
        {
            x /= 128;
            length++;
        } while (x > 0);

        return length;
    }
}

