using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;

namespace Broker.Core.PacketHandlers;

/// <summary>
/// Handler for PUBACK packets.
/// </summary>
public class PubAckHandler(IPendingStore pendingStore) : PacketHandlerBase<MqttPubAckPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPubAckPacket packet, CancellationToken cancellationToken)
    {
        await pendingStore.RemovePendingAsync(connection.ClientId, packet.PacketId, cancellationToken);
    }
}

/// <summary>
/// Handler for PUBREC packets.
/// </summary>
public class PubRecHandler(IPendingStore pendingStore) : PacketHandlerBase<MqttPubRecPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPubRecPacket packet, CancellationToken cancellationToken)
    {
        var st = await pendingStore.GetQos2StateAsync(connection.ClientId, packet.PacketId, cancellationToken);
        if (st != null && st.State == Qos2State.WaitingForPubRec)
        {
            st.State = Qos2State.WaitingForPubComp;
            await pendingStore.AddPendingAsync(connection.ClientId, packet.PacketId, st.Packet, cancellationToken, st.State);
            var pubRel = new MqttPubRelPacket { PacketId = packet.PacketId };
            await connection.SendAsync(pubRel, cancellationToken);
        }
    }
}

/// <summary>
/// Handler for PUBREL packets.
/// </summary>
public class PubRelHandler(IPendingStore pendingStore, IMessageRouter router) : PacketHandlerBase<MqttPubRelPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPubRelPacket packet, CancellationToken cancellationToken)
    {
        // deliver to subscribers then send PUBCOMP
        var st = await pendingStore.GetQos2StateAsync(connection.ClientId, packet.PacketId, cancellationToken);
        if (st != null)
        {
            await router.RouteAsync(st.Packet, cancellationToken);
            var pubComp = new MqttPubCompPacket { PacketId = packet.PacketId };
            await connection.SendAsync(pubComp, cancellationToken);
            await pendingStore.RemoveQos2StateAsync(connection.ClientId, packet.PacketId, cancellationToken);
        }
    }
}

/// <summary>
/// Handler for PUBCOMP packets.
/// </summary>
public class PubCompHandler(IPendingStore pendingStore) : PacketHandlerBase<MqttPubCompPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPubCompPacket packet, CancellationToken cancellationToken)
    {
        await pendingStore.RemoveQos2StateAsync(connection.ClientId, packet.PacketId, cancellationToken);
    }
}

