using Broker.Core;
using Broker.Core.Packets;
using Broker.Core.Routing;

namespace Broker.Frontend;

public class PacketDispatcher(
    IPacketHandler<MqttConnectPacket, MqttConnAckPacket> connect,
    IPacketHandler<MqttPublishPacket> publish,
    IPacketHandler<MqttSubscribePacket, MqttSubAckPacket> subscribe,
    IPacketHandler<MqttUnsubscribePacket, MqttUnsubAckPacket> unsubscribe,
    IPacketHandler<MqttPubAckPacket> pubAckHandler,
    IPacketHandler<MqttPubRecPacket> pubRecHandler,
    IPacketHandler<MqttPubRelPacket> pubRelHandler,
    IPacketHandler<MqttPubCompPacket> pubCompHandler,
    IMqttTcpServer server)
{
    private readonly Dictionary<MqttPacketType, IPacketHandler> _handlers = new()
    {
        { MqttPacketType.CONNECT, connect },
        { MqttPacketType.PUBLISH, publish },
        { MqttPacketType.SUBSCRIBE, subscribe },
        { MqttPacketType.UNSUBSCRIBE, unsubscribe },
        { MqttPacketType.PUBACK, pubAckHandler },
        { MqttPacketType.PUBREC, pubRecHandler },
        { MqttPacketType.PUBREL, pubRelHandler },
        { MqttPacketType.PUBCOMP, pubCompHandler }
    };

    public async Task DispatchAsync(MqttClientConnection connection, MqttPacket packet, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(packet.PacketType, out var handler))
        {
            throw new NotImplementedException($"No handler registered for packet type: {packet.PacketType}");
        }

        var response = await handler.HandleAsync(connection, packet, cancellationToken);

        // Send response packet if one was returned
        // some types of handles (mainly for QoS 2 messages) do not response message for answer
        if (response != null)
        {
            await connection.SendAsync(response, cancellationToken);
        }

        // Special handling for CONNECT - register connection if accepted
        if (packet.PacketType == MqttPacketType.CONNECT && response is MqttConnAckPacket connAck)
        {
            if (connAck.ReturnCode == MqttConnectReturnCode.Accepted)
            {
                await server.RegisterClientConnectionAsync(connection.ClientId, connection);
            }
        }
    }
}
