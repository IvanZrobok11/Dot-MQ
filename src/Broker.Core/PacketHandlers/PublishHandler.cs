using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

public class PublishHandler(IMessageRouter router,
    IIncomingPublishQosHandler incomingPublishQosHandler,
    IRetainedStore retained,
    ILogger<PublishHandler> logger,
    IBrokerMetricsService metrics) : PacketHandlerBase<MqttPublishPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPublishPacket packet, CancellationToken ct)
    {
        logger.LogDebug("Publish from {ClientId} to {Topic} (QoS {Qos})", connection.ClientId, packet.TopicName, packet.QoS);
        metrics.RecordMessageReceived();
        metrics.RecordMessagePublished();

        if (packet.Retain)
        {
            if (packet.Payload != null && packet.Payload.Length > 0)
            {
                await retained.SaveAsync(packet, ct);
                logger.LogDebug("Stored retained message for {Topic}", packet.TopicName);
            }
            else
            {
                await retained.DeleteAsync(packet.TopicName, ct);
                logger.LogDebug("Deleted retained message for {Topic}", packet.TopicName);
            }
        }

        if (packet.QoS == QoSLevel.AtMostOnce)
        {
            await router.RouteAsync(packet, ct);
            return;
        }

        await incomingPublishQosHandler.HandleAsync(connection, packet, ct);
    }
}

