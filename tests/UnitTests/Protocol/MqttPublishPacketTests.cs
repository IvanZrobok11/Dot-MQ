using Broker.Core;
using Broker.Core.Packets;
using FluentAssertions;
using System.Text;

namespace TestProject1.Protocol;

public class MqttPublishPacketTests
{
    [Fact]
    public void SerializeDeserialize_AllAttributes_ShouldPreserveAllProperties()
    {
        var packet = new MqttPublishPacket
        {
            TopicName = "comprehensive/test/topic/with/multiple/levels",
            QoS = QoSLevel.ExactlyOnce,
            PacketId = 65535,
            Retain = true,
            Duplicate = true,
            Payload = Encoding.UTF8.GetBytes("This is a comprehensive test message with special chars: !@#$%^&*()")
        };

        var bytes = packet.Serialize();
        var deserialized = MqttPublishPacket.Deserialize(bytes);

        deserialized.Should().BeEquivalentTo(packet);
    }
}

