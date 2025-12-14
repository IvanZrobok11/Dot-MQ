using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

public class UnsubscribeHandler(
    ISubscriptionManager subs,
    ISessionStore sessions,
    ILogger<UnsubscribeHandler> logger) : PacketHandlerBase<MqttUnsubscribePacket, MqttUnsubAckPacket>
{
    public override async Task<MqttUnsubAckPacket?> HandleAsync(IMqttConnection connection, MqttUnsubscribePacket packet, CancellationToken ct)
    {
        logger.LogDebug("Unsubscribe request from {ClientId}", connection.ClientId);

        var session = await sessions.GetSessionAsync(connection.ClientId, ct);
        if (session != null)
        {
            foreach (var topic in packet.TopicFilters)
            {
                subs.Unsubscribe(connection.ClientId, topic);
                session.Subscriptions.Remove(topic);
            }
            await sessions.SaveSessionAsync(session, ct);
        }

        return new MqttUnsubAckPacket { PacketId = packet.PacketId };
    }
}

