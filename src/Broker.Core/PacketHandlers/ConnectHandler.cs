using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

public class ConnectHandler(
    ISessionStore sessionStore,
    ISubscriptionManager subs,
    IIncomingPublishQosHandler incomingPublishQosHandler,
    ILogger<ConnectHandler> logger,
    IPendingStore pendingStore,
    IBrokerMetricsService metrics) : PacketHandlerBase<MqttConnectPacket, MqttConnAckPacket>
{
    public override async Task<MqttConnAckPacket?> HandleAsync(IMqttConnection connection, MqttConnectPacket packet, CancellationToken ct)
    {
        logger.LogDebug("Processing CONNECT from {ClientId}", packet.ClientId);

        if (packet.ProtocolLevel != 4)
        {
            logger.LogWarning("Unsupported protocol version {ProtocolLevel} from {ClientId}", packet.ProtocolLevel, packet.ClientId);
            return new MqttConnAckPacket { SessionPresent = false, ReturnCode = MqttConnectReturnCode.UnacceptableProtocolVersion };
        }

        var existing = await sessionStore.GetSessionAsync(packet.ClientId, ct);
        bool sessionPresent = false;

        if (packet.CleanSession)
        {
            if (existing != null)
            {
                await sessionStore.DeleteSessionAsync(packet.ClientId, ct);
                subs.UnsubscribeAll(packet.ClientId);
                logger.LogDebug("Cleaned existing session for {ClientId}", packet.ClientId);
            }
        }
        else if (existing != null)
        {
            sessionPresent = true;
            foreach (var (topic, qos) in existing.Subscriptions)
            {
                subs.Subscribe(packet.ClientId, topic, qos);
            }

            // deliver pending messages via qos engine
            var pendings = await pendingStore.GetPendingMessagesAsync(packet.ClientId, ct);
            foreach (var p in pendings)
            {
                var publish = new MqttPublishPacket
                {
                    TopicName = p.Topic,
                    Payload = p.Payload,
                    QoS = p.Qos,
                    PacketId = p.PacketId,
                    Retain = false,
                    Duplicate = true
                };
                await incomingPublishQosHandler.HandleAsync(connection, publish, ct);
            }
        }

        var session = new MqttSession
        {
            ClientId = packet.ClientId,
            CleanSession = packet.CleanSession,
            Subscriptions = existing?.Subscriptions ?? new Dictionary<string, QoSLevel>()
        };

        if (packet.WillFlag && !string.IsNullOrEmpty(packet.WillTopic))
        {
            session.WillMessage = new WillMessage(packet.WillTopic, packet.WillMessage, packet.WillQos, packet.WillRetain);
        }

        await sessionStore.SaveSessionAsync(session, ct);
        metrics?.RecordMessageReceived();

        logger.LogInformation("Client {ClientId} connected (SessionPresent: {SessionPresent})", packet.ClientId, sessionPresent);

        return new MqttConnAckPacket { SessionPresent = sessionPresent, ReturnCode = MqttConnectReturnCode.Accepted };
    }
}

