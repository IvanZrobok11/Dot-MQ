using Broker.Core;
using Broker.Core.Packets;
using FluentAssertions;
using System.Text;

namespace TestProject1.Protocol;

public class MqttConnectPacketTests
{
    [Fact]
    public void SerializeDeserialize_AllAttributes_ShouldPreserveAllProperties()
    {
        var packet = new MqttConnectPacket
        {
            ProtocolName = "MQTT",
            ProtocolLevel = 4,
            ClientId = "comprehensive-test-client-id",
            KeepAlive = 300,
            CleanSession = true,
            WillFlag = true,
            WillTopic = "test/will/topic/path",
            WillMessage = Encoding.UTF8.GetBytes("This is a comprehensive will message with special chars: !@#$%^&*()"),
            WillQos = QoSLevel.ExactlyOnce,
            WillRetain = true,
            UsernameFlag = true,
            Username = "comprehensive-test-username",
            PasswordFlag = true,
            Password = Encoding.UTF8.GetBytes("comprehensive-test-password-12345")
        };

        var bytes = packet.Serialize();
        var deserialized = MqttConnectPacket.Deserialize(bytes);

        deserialized.Should().BeEquivalentTo(packet);
    }
}

