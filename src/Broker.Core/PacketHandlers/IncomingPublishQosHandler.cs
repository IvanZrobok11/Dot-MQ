using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

/// <summary>
/// Handles incoming PUBLISH packets with QoS 1 or 2.
/// </summary>
public interface IIncomingPublishQosHandler
{
    Task HandleAsync(IMqttConnection connection, MqttPublishPacket packet, CancellationToken cancellationToken);
}

public class IncomingPublishQosHandler(
    IPendingStore pendingStore,
    PacketIdManager packetIdManager,
    IMessageRouter router,
    ILogger<IncomingPublishQosHandler> logger) : IIncomingPublishQosHandler
{
    public async Task HandleAsync(IMqttConnection connection, MqttPublishPacket packet, CancellationToken cancellationToken)
    {
        if (packet.QoS == QoSLevel.AtLeastOnce)
        {
            var id = packet.PacketId ?? packetIdManager.GetNextPacketId(connection.ClientId);
            await pendingStore.AddPendingAsync(connection.ClientId, id, packet, cancellationToken);
            var pubAck = new MqttPubAckPacket { PacketId = id };
            await connection.SendAsync(pubAck, cancellationToken);
            await router.RouteAsync(packet, cancellationToken);
        }
        else if (packet.QoS == QoSLevel.ExactlyOnce)
        {
            var id = packet.PacketId ?? packetIdManager.GetNextPacketId(connection.ClientId);
            await pendingStore.AddPendingAsync(connection.ClientId, id, packet, cancellationToken, Qos2State.WaitingForPubRec);
            var pubRec = new MqttPubRecPacket { PacketId = id };
            await connection.SendAsync(pubRec, cancellationToken);
        }
    }
}

