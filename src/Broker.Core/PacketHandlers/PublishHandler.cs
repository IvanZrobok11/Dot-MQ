using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

public class PublishHandler(IMessageRouter messageRouter,
    IIncomingPublishQosHandler incomingPublishQosHandler,
    IRetainedStore retainedStore,
    ILogger<PublishHandler> logger,
    IBrokerMetricsService metrics) : PacketHandlerBase<MqttPublishPacket>
{
    public override async Task HandleAsync(IMqttConnection connection, MqttPublishPacket packet, CancellationToken cancellationToken)
    {
        logger.LogDebug("Publish from {ClientId} to {Topic} (QoS {Qos})", connection.ClientId, packet.TopicName, packet.QoS);
        metrics.RecordMessageReceived();
        metrics.RecordMessagePublished();

        // retain mark to send last message to new subscribers (after publishing)
        if (packet.Retain)
        {
            if (packet.Payload != null && packet.Payload.Length > 0)
            {
                await retainedStore.SaveAsync(packet, cancellationToken);
                logger.LogDebug("Stored retained message for {Topic}", packet.TopicName);
            }
            else
            {
                await retainedStore.DeleteAsync(packet.TopicName, cancellationToken);
                logger.LogDebug("Deleted retained message for {Topic}", packet.TopicName);
            }
        }

        if (packet.QoS == QoSLevel.AtMostOnce)
        {
            await messageRouter.RouteAsync(packet, cancellationToken);
            return;
        }

        await incomingPublishQosHandler.HandleAsync(connection, packet, cancellationToken);
    }
}

