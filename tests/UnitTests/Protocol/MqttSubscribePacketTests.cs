using Broker.Core;
using Broker.Core.Packets;
using FluentAssertions;

namespace TestProject1.Protocol;

public class MqttSubscribePacketTests
{
    [Fact]
    public void SerializeDeserialize_AllAttributes_ShouldPreserveAllProperties()
    {
        var packet = new MqttSubscribePacket
        {
            PacketId = 65535,
            Subscriptions = new List<(string, QoSLevel)>
            {
                ("comprehensive/test/topic/simple", QoSLevel.AtMostOnce),
                ("sensors/+/temperature", QoSLevel.AtLeastOnce),
                ("sensors/#", QoSLevel.ExactlyOnce),
                ("control/+/+/action", QoSLevel.AtMostOnce),
                ("devices/+/status/+", QoSLevel.AtLeastOnce),
                ("root/level/with/many/segments", QoSLevel.ExactlyOnce)
            }
        };

        var bytes = packet.Serialize();
        var deserialized = MqttSubscribePacket.Deserialize(bytes);

        deserialized.Should().BeEquivalentTo(packet);
    }
}

