using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Broker.Core.Routing;

public interface ISubscribeService
{
    Task<MqttSubAckPacket> HandleAsync(IMqttConnection connection, MqttSubscribePacket packet, CancellationToken ct);
}

public class SubscribeService(ISubscriptionManager subs,
    ISessionStore sessions,
    IRetainedStore retained,
    PacketIdManager packetIds,
    IMessageRouter messageRouter,
    ILogger<SubscribeService> logger) : ISubscribeService
{
    private readonly IRetainedStore _retained = retained;

    public async Task<MqttSubAckPacket> HandleAsync(IMqttConnection connection, MqttSubscribePacket packet, CancellationToken ct)
    {
        logger.LogDebug("Subscribe request from {ClientId} with {Count} filters", connection.ClientId, packet.Subscriptions.Count);
        var results = new List<QoSLevel>();
        var session = await sessions.GetSessionAsync(connection.ClientId, ct)
            ?? new MqttSession { ClientId = connection.ClientId, Subscriptions = new Dictionary<string, QoSLevel>() };

        foreach (var (topicFilter, requestedQos) in packet.Subscriptions)
        {
            try
            {
                var granted = subs.Subscribe(connection.ClientId, topicFilter, requestedQos);
                results.Add(granted);
                session.Subscriptions[topicFilter] = granted;
                logger.LogDebug("Client {ClientId} subscribed to {Topic} (granted {Qos})", connection.ClientId, topicFilter, granted);

                // deliver retained messages matching filter
                var retained = await _retained.MatchAsync(topicFilter, ct);
                foreach (var r in retained)
                {
                    var p = new MqttPublishPacket
                    {
                        TopicName = r.Topic,
                        Payload = r.Payload,
                        QoS = r.Qos,
                        Retain = true,
                        PacketId = r.Qos > QoSLevel.AtMostOnce ? packetIds.GetNextPacketId(connection.ClientId) : null
                    };
                    // route to single client via router (router should use client registry to send)
                    await messageRouter.RouteAsync(p, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to subscribe {ClientId} to {Topic}", connection.ClientId, topicFilter);
                results.Add((QoSLevel)0x80);
            }
        }

        await sessions.SaveSessionAsync(session, ct);

        return new MqttSubAckPacket
        {
            PacketId = packet.PacketId,
            GrantedQosLevels = results
        };
    }
}
