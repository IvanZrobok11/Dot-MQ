using Broker.Core.Packets;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Broker.Core.Qos;

public interface IQosFlowEngine
{
    Task HandleIncomingPublishAsync(IMqttConnection client, MqttPublishPacket packet, CancellationToken ct);
    Task HandlePubAckAsync(IMqttConnection connection, MqttPubAckPacket packet, CancellationToken ct);
    Task HandlePubRecAsync(IMqttConnection connection, MqttPubRecPacket packet, CancellationToken ct);
    Task HandlePubRelAsync(IMqttConnection connection, MqttPubRelPacket packet, CancellationToken ct);
    Task HandlePubCompAsync(IClient client, MqttPubCompPacket packet, CancellationToken ct);
}

public class QosFlowEngine(
    IPendingStore pendingStore,
    PacketIdManager packetIdManager,
    IMessageRouter router,
    ILogger<QosFlowEngine> logger) : IQosFlowEngine
{
    public async Task HandleIncomingPublishAsync(IMqttConnection connection, MqttPublishPacket packet, CancellationToken ct)
    {
        if (packet.QoS == QoSLevel.AtLeastOnce)
        {
            var id = packet.PacketId ?? packetIdManager.GetNextPacketId(connection.ClientId);
            await pendingStore.AddPendingAsync(connection.ClientId, id, packet, ct);
            var pubAck = new MqttPubAckPacket { PacketId = id };
            await connection.SendAsync(pubAck, ct);
            await router.RouteAsync(packet, ct);
        }
        else if (packet.QoS == QoSLevel.ExactlyOnce)
        {
            var id = packet.PacketId ?? packetIdManager.GetNextPacketId(connection.ClientId);
            await pendingStore.AddPendingAsync(connection.ClientId, id, packet, ct, Qos2State.WaitingForPubRec);
            var pubRec = new MqttPubRecPacket { PacketId = id };
            await connection.SendAsync(pubRec, ct);
        }
    }

    public async Task HandlePubAckAsync(IMqttConnection connection, MqttPubAckPacket packet, CancellationToken ct)
    {
        await pendingStore.RemovePendingAsync(connection.ClientId, packet.PacketId, ct);
    }

    public async Task HandlePubRecAsync(IMqttConnection connection, MqttPubRecPacket packet, CancellationToken ct)
    {
        var st = await pendingStore.GetQos2StateAsync(connection.ClientId, packet.PacketId, ct);
        if (st != null && st.State == Qos2State.WaitingForPubRec)
        {
            st.State = Qos2State.WaitingForPubComp;
            await pendingStore.AddPendingAsync(connection.ClientId, packet.PacketId, st.Packet, ct, st.State);
            var pubRel = new MqttPubRelPacket { PacketId = packet.PacketId };
            await connection.SendAsync(pubRel, ct);
        }
    }

    public async Task HandlePubRelAsync(IMqttConnection connection, MqttPubRelPacket packet, CancellationToken ct)
    {
        // deliver to subscribers then send PUBCOMP
        var st = await pendingStore.GetQos2StateAsync(connection.ClientId, packet.PacketId, ct);
        if (st != null)
        {
            await router.RouteAsync(st.Packet, ct);
            var pubComp = new MqttPubCompPacket { PacketId = packet.PacketId };
            await connection.SendAsync(pubComp, ct);
            await pendingStore.RemoveQos2StateAsync(connection.ClientId, packet.PacketId, ct);
        }
    }

    public async Task HandlePubCompAsync(IClient client, MqttPubCompPacket packet, CancellationToken ct)
    {
        await pendingStore.RemoveQos2StateAsync(client.ClientId, packet.PacketId, ct);
    }
}

//// Broker.Core/Qos/InMemoryQosFlowEngine.cs (приклад)
//public class InMemoryQosFlowEngine : IQosFlowEngine
//{
//    private readonly IPendingStore _pending;
//    private readonly IMessageSender _sender;
//    private readonly ILogger<InMemoryQosFlowEngine> _logger;

//    public InMemoryQosFlowEngine(IPendingStore pending, IMessageSender sender, ILogger<InMemoryQosFlowEngine> logger)
//    {
//        _pending = pending;
//        _sender = sender;
//        _logger = logger;
//    }

//    public async Task HandleIncomingPublishAsync(IClientConnection connection, MqttPublishPacket packet, CancellationToken ct)
//    {
//        switch (packet.Qos)
//        {
//            case QoSLevel.AtLeastOnce:
//                var id = packet.PacketId ?? connection.PacketIdManager.GetNextPacketId(connection.ClientId);
//                await _pending.AddPendingAsync(connection.ClientId, id, packet, ct);
//                var pubAck = new MqttPubAckPacket { PacketId = id };
//                await _sender.SendAsync(pubAck, ct);
//                // route to subscribers (may be asynchronous)
//                await connection.Router.RouteAsync(packet, ct);
//                break;
//            case QoSLevel.ExactlyOnce:
//                var pid = packet.PacketId ?? connection.PacketIdManager.GetNextPacketId(connection.ClientId);
//                // persist state (WAIT_FOR_PUBREC)
//                await _pending.SaveQos2StateAsync(connection.ClientId, pid, packet, Qos2State.WaitingForPubRec, ct);
//                var pubRec = new MqttPubRecPacket { PacketId = pid };
//                await _sender.SendAsync(pubRec, ct);
//                break;
//        }
//    }

//    // Other handlers: HandlePubAckAsync, HandlePubRecAsync, HandlePubRelAsync, HandlePubCompAsync ...
//}

//Один QoS 2 message = один PacketId + один State
//public enum Qos2State
//{
//    WaitingForPubRec,   // після PUBLISH
//    WaitingForPubRel,   // після PUBREC (subscriber side)
//    WaitingForPubComp,  // після PUBREL
//    Completed
//}
//public sealed class Qos2StateEntity
//{
//    public string ClientId { get; init; } = default!;
//    public ushort PacketId { get; init; }

//    public string Topic { get; init; } = default!;
//    public byte[] Payload { get; init; } = default!;
//    public QoSLevel Qos { get; init; }
//    public bool Retain { get; init; }

//    public Qos2State State { get; set; }

//    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
//    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
//}
