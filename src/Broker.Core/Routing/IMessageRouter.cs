using Broker.Core.Packets;

namespace Broker.Core.Routing;

/// <summary>
/// Interface for routing MQTT PUBLISH messages to subscribers.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Routes a PUBLISH message to all matching subscribers.
    /// </summary>
    /// <param name="publishPacket">The PUBLISH packet to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RouteAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken);
}

