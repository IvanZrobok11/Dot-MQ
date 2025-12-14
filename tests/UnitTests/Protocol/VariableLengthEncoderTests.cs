using Broker.Core;
using FluentAssertions;

namespace TestProject1.Protocol;

public class VariableLengthEncoderTests
{
    [Fact]
    public void Encode_Zero_ReturnsSingleByte()
    {
        var result = VariableLengthEncoder.Encode(0);

        result.Should().HaveCount(1);
        result[0].Should().Be(0x00);
    }

    [Fact]
    public void Encode_SmallValue_ReturnsSingleByte()
    {
        var result = VariableLengthEncoder.Encode(127);

        result.Should().HaveCount(1);
        result[0].Should().Be(0x7F);
    }

    [Fact]
    public void Encode_Value128_ReturnsTwoBytes()
    {
        var result = VariableLengthEncoder.Encode(128);

        result.Should().HaveCount(2);
        result[0].Should().Be(0x80);
        result[1].Should().Be(0x01);
    }

    [Fact]
    public void Encode_Value16383_ReturnsTwoBytes()
    {
        var result = VariableLengthEncoder.Encode(16383);

        result.Should().HaveCount(2);
        result[0].Should().Be(0xFF);
        result[1].Should().Be(0x7F);
    }

    [Fact]
    public void Encode_LargeValue_ReturnsMultipleBytes()
    {
        var result = VariableLengthEncoder.Encode(2097151); // 0x1FFFFF

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Encode_ValueOutOfRange_ThrowsException()
    {
        Action act = () => VariableLengthEncoder.Encode(268435456); // Max + 1

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Decode_SingleByte_ReturnsCorrectValue()
    {
        using var stream = new MemoryStream(new byte[] { 0x7F });
        using var reader = new BinaryReader(stream);

        var result = VariableLengthEncoder.Decode(reader);

        result.Should().Be(127);
    }

    [Fact]
    public void Decode_TwoBytes_ReturnsCorrectValue()
    {
        using var stream = new MemoryStream(new byte[] { 0x80, 0x01 });
        using var reader = new BinaryReader(stream);

        var result = VariableLengthEncoder.Decode(reader);

        result.Should().Be(128);
    }

    [Fact]
    public void EncodeDecode_RoundTrip_Works()
    {
        int[] testValues = { 0, 1, 127, 128, 16383, 16384, 2097151, 268435455 };

        foreach (var value in testValues)
        {
            var encoded = VariableLengthEncoder.Encode(value);
            using var stream = new MemoryStream(encoded);
            using var reader = new BinaryReader(stream);
            var decoded = VariableLengthEncoder.Decode(reader);

            decoded.Should().Be(value);
        }
    }

    [Fact]
    public void GetEncodedLength_ReturnsCorrectLength()
    {
        VariableLengthEncoder.GetEncodedLength(0).Should().Be(1);
        VariableLengthEncoder.GetEncodedLength(127).Should().Be(1);
        VariableLengthEncoder.GetEncodedLength(128).Should().Be(2);
        VariableLengthEncoder.GetEncodedLength(16383).Should().Be(2);
        VariableLengthEncoder.GetEncodedLength(16384).Should().Be(3);
    }
}

