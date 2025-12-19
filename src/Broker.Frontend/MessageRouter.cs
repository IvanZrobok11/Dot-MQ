using Broker.Core;
using Broker.Core.Packets;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Broker.Frontend;

/// <summary>
/// Routes MQTT PUBLISH messages to subscribers.
/// </summary>
public class MessageRouter(
        ISubscriptionManager subscriptionManager,
        IBrokerMetricsService metricsService,
        ILogger<MessageRouter> logger) : IMessageRouter
{
    /// <summary>
    /// After the publisher publishes the message and responds to the pub act
    /// this function is called
    /// Sends the message to all online subscribers
    /// </summary>
    public async Task RouteAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
    {
        if (publishPacket == null)
        {
            throw new ArgumentNullException(nameof(publishPacket));
        }

        // in publish we cannot use withdraw unlike subscribe
        if (!TopicMatcher.IsValidTopicName(publishPacket.TopicName))
        {
            logger?.LogWarning("Invalid topic name: {TopicName}", publishPacket.TopicName);
            return;
        }

        // Handle retained messages (storage will be handled at a higher level)
        if (publishPacket.Retain)
        {
            logger?.LogDebug("Retained message published to topic {TopicName}", publishPacket.TopicName);
        }

        // Find all matching subscribers
        var subscribers = subscriptionManager.GetSubscribers(publishPacket.TopicName);

        if (subscribers.Count == 0)
        {
            logger?.LogDebug("No subscribers found for topic {TopicName}", publishPacket.TopicName);
            return;
        }

        // Route to each subscriber
        var routingTasks = new List<Task>();

        foreach (var subscription in subscribers)
        {
            // Determine the QoS to use (min of publish QoS and subscription QoS)
            QoSLevel deliveryQos = (QoSLevel)Math.Min((byte)publishPacket.QoS, (byte)subscription.GrantedQos);

            // Create a PUBLISH packet with the appropriate QoS for this subscriber
            var subscriberPacket = CreateSubscriberPublishPacket(publishPacket, deliveryQos);

            //TODO: qos 1 and 2
            // Route to subscriber - use QoS 1 handler if QoS is 1
            //if (deliveryQos == QoSLevel.AtLeastOnce)
            //{
            //    routingTasks.Add(HandlePublishAsync(subscription.ClientId, subscriberPacket, cancellationToken));
            //}
            //else
            {
                routingTasks.Add(RouteToSubscriberAsync(subscription, subscriberPacket, cancellationToken));
            }
        }

        // Wait for all routing tasks to complete
        await Task.WhenAll(routingTasks);
    }


    private MqttPublishPacket CreateSubscriberPublishPacket(MqttPublishPacket originalPacket, QoSLevel deliveryQos)
    {
        var subscriberPacket = new MqttPublishPacket
        {
            TopicName = originalPacket.TopicName,
            Payload = originalPacket.Payload,
            QoS = deliveryQos,
            Retain = false, // Retain flag is not forwarded to subscribers
            Duplicate = originalPacket.Duplicate
        };

        // Packet ID is required for QoS > 0
        if (deliveryQos > QoSLevel.AtMostOnce)
        {
            // Packet ID will be assigned by the sender or broker core
            // For now, we'll use the original packet ID if available
            subscriberPacket.PacketId = originalPacket.PacketId;
        }

        return subscriberPacket;
    }

    private async Task RouteToSubscriberAsync(Subscription subscription, MqttPublishPacket packet, CancellationToken cancellationToken)
    {
        var connections = MqttTcpServer.ConnectionsByClientId;
        if (!connections.TryGetValue(subscription.ClientId, out var sender))
        {
            logger.LogWarning("No message sender found for client {ClientId}", subscription.ClientId);
            return;
        }

        if (!sender.IsConnected)
        {
            logger.LogDebug("Client {ClientId} is not connected, skipping message delivery", subscription.ClientId);

            // For QoS 1/2, we should queue the message for later delivery
            // This will be handled at a higher level when we implement QoS 1/2 support
            if (packet.QoS > QoSLevel.AtMostOnce)
            {
                logger.LogDebug("Client {ClientId} is offline, message will be queued for QoS {Qos}",
                    subscription.ClientId, packet.QoS);
            }
            return;
        }

        try
        {
            await sender.SendAsync(packet, cancellationToken);
            logger?.LogDebug("Routed message to client {ClientId} on topic {TopicName} with QoS {Qos}",
                subscription.ClientId, packet.TopicName, packet.QoS);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error routing message to client {ClientId}", subscription.ClientId);

            // For QoS 1/2, queue the message for retry (handled at higher level)
            if (packet.QoS > QoSLevel.AtMostOnce)
            {
                logger.LogDebug("Error sending message to client {ClientId}, message will be queued for QoS {Qos}",
                    subscription.ClientId, packet.QoS);
            }
        }
    }

}

