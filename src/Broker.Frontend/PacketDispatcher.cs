using Broker.Core;
using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Broker.Frontend;

public class PacketDispatcher(
    IConnectService connect,
    IPublishService publish,
    ISubscribeService subscribe,
    IUnsubscribeService unsubscribe,
    IQosFlowEngine qos,
    IMqttTcpServer server,
    ILogger<PacketDispatcher> logger)
{
    public async Task DispatchAsync(MqttClientConnection connection, MqttPacket packet, CancellationToken cancellationToken)
    {
        switch (packet.PacketType)
        {
            case MqttPacketType.CONNECT:
                var connAck = await connect.HandleAsync(connection, (MqttConnectPacket)packet, cancellationToken);
                await connection.SendAsync(connAck, cancellationToken);

                // Register connection if CONNECT was accepted
                if (connAck.ReturnCode == MqttConnectReturnCode.Accepted)
                {
                    await server.RegisterClientConnectionAsync(connection.ClientId, connection);
                }
                break;
            case MqttPacketType.PUBLISH:
                await publish.HandleAsync(connection, (MqttPublishPacket)packet, cancellationToken);
                break;
            case MqttPacketType.SUBSCRIBE:
                var subAck = await subscribe.HandleAsync(connection, (MqttSubscribePacket)packet, cancellationToken);
                await connection.SendAsync(subAck, cancellationToken);
                break;
            case MqttPacketType.UNSUBSCRIBE:
                var unsubAck = await unsubscribe.HandleAsync(connection, (MqttUnsubscribePacket)packet, cancellationToken);
                await connection.SendAsync(unsubAck, cancellationToken);
                break;
            case MqttPacketType.PUBACK:
                await qos.HandlePubAckAsync(connection, (MqttPubAckPacket)packet, cancellationToken);
                break;
            case MqttPacketType.PUBREC:
                await qos.HandlePubRecAsync(connection, (MqttPubRecPacket)packet, cancellationToken);
                break;
            case MqttPacketType.PUBREL:
                await qos.HandlePubRelAsync(connection, (MqttPubRelPacket)packet, cancellationToken);
                break;
            case MqttPacketType.PUBCOMP:
                await qos.HandlePubCompAsync(connection, (MqttPubCompPacket)packet, cancellationToken);
                break;
            default:
                throw new NotImplementedException(nameof(packet.PacketType));
        }
    }
}
