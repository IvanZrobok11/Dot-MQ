using Broker.Core;
using Broker.Core.Packets;
using FluentAssertions;
using System.Text;

namespace TestProject1.Protocol;

public class MqttPacketParserTests
{
    [Fact]
    public void Parse_CONNECT_ShouldReturnConnectPacket()
    {
        var packet = new MqttConnectPacket
        {
            ClientId = "test-client",
            CleanSession = true,
            KeepAlive = 60
        };

        var bytes = packet.Serialize();
        var parsed = MqttPacketParser.Parse(bytes);

        parsed.Should().BeOfType<MqttConnectPacket>();
        ((MqttConnectPacket)parsed).ClientId.Should().Be("test-client");
    }

    [Fact]
    public void Parse_PUBLISH_ShouldReturnPublishPacket()
    {
        var packet = new MqttPublishPacket
        {
            TopicName = "test/topic",
            QoS = QoSLevel.AtMostOnce,
            Payload = Encoding.UTF8.GetBytes("message")
        };

        var bytes = packet.Serialize();
        var parsed = MqttPacketParser.Parse(bytes);

        parsed.Should().BeOfType<MqttPublishPacket>();
        ((MqttPublishPacket)parsed).TopicName.Should().Be("test/topic");
    }

    [Fact]
    public void Parse_SUBSCRIBE_ShouldReturnSubscribePacket()
    {
        var packet = new MqttSubscribePacket
        {
            PacketId = 1,
            Subscriptions = new List<(string, QoSLevel)> { ("test/topic", QoSLevel.AtLeastOnce) }
        };

        var bytes = packet.Serialize();
        var parsed = MqttPacketParser.Parse(bytes);

        parsed.Should().BeOfType<MqttSubscribePacket>();
    }

    [Fact]
    public void Parse_PINGREQ_ShouldReturnPingReqPacket()
    {
        var packet = new MqttPingReqPacket();
        var bytes = packet.Serialize();
        var parsed = MqttPacketParser.Parse(bytes);

        parsed.Should().BeOfType<MqttPingReqPacket>();
    }

    [Fact]
    public void Parse_EmptyData_ShouldThrow()
    {
        Action act = () => MqttPacketParser.Parse(Array.Empty<byte>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_TooShortData_ShouldThrow()
    {
        Action act = () => MqttPacketParser.Parse(new byte[] { 0x10 });

        act.Should().Throw<ArgumentException>();
    }
}

