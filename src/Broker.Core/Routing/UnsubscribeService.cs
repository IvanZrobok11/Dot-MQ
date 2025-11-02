using Broker.Core.Packets;
using Broker.Core.Qos;
using Microsoft.Extensions.Logging;

namespace Broker.Core.Routing;

public interface IUnsubscribeService
{
    Task<MqttUnsubAckPacket> HandleAsync(IClient connection, MqttUnsubscribePacket packet, CancellationToken ct);
}

public class UnsubscribeService(
    ISubscriptionManager subs,
    ISessionStore sessions,
    ILogger<UnsubscribeService> logger) : IUnsubscribeService
{
    public async Task<MqttUnsubAckPacket> HandleAsync(IClient connection, MqttUnsubscribePacket packet, CancellationToken ct)
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