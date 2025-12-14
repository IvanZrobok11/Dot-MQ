using Broker.Core;
using Broker.Core.Packets;

namespace Broker.Core.Routing;

/// <summary>
/// Non-generic base interface for handling MQTT packets.
/// </summary>
public interface IPacketHandler
{
    /// <summary>
    /// Handles an incoming MQTT packet.
    /// </summary>
    /// <param name="connection">The client connection.</param>
    /// <param name="packet">The packet to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response packet, or null if no response is needed.</returns>
    Task<MqttPacket?> HandleAsync(IMqttConnection connection, MqttPacket packet, CancellationToken cancellationToken);
}

/// <summary>
/// Generic interface for handling MQTT packets with no output.
/// </summary>
public interface IPacketHandler<TIncomingPackage> : IPacketHandler
    where TIncomingPackage : MqttPacket
{
    /// <summary>
    /// Handles an incoming MQTT packet.
    /// </summary>
    /// <param name="connection">The client connection.</param>
    /// <param name="packet">The packet to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(IMqttConnection connection, TIncomingPackage packet, CancellationToken cancellationToken);
}

/// <summary>
/// Generic interface for handling MQTT packets with type safety and output.
/// </summary>
public interface IPacketHandler<TIncomingPackage, TOutgoingPackage> : IPacketHandler
    where TIncomingPackage : MqttPacket
    where TOutgoingPackage : MqttPacket
{
    /// <summary>
    /// Handles an incoming MQTT packet.
    /// </summary>
    /// <param name="connection">The client connection.</param>
    /// <param name="packet">The packet to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response packet, or null if no response is needed.</returns>
    Task<TOutgoingPackage?> HandleAsync(IMqttConnection connection, TIncomingPackage packet, CancellationToken cancellationToken);
}

