using Broker.Core.Packets;
using Broker.Core.Routing;

namespace Broker.Core.PacketHandlers;

/// <summary>
/// Base class for packet handlers with no output.
/// </summary>
public abstract class PacketHandlerBase<TIncomingPackage> : IPacketHandler<TIncomingPackage>
    where TIncomingPackage : MqttPacket
{
    public async Task<MqttPacket?> HandleAsync(IMqttConnection connection, MqttPacket packet, CancellationToken cancellationToken)
    {
        if (packet is not TIncomingPackage typedPacket)
        {
            throw new ArgumentException($"Expected {typeof(TIncomingPackage).Name}, got {packet.GetType().Name}", nameof(packet));
        }
        await HandleAsync(connection, typedPacket, cancellationToken);
        return null;
    }

    public abstract Task HandleAsync(IMqttConnection connection, TIncomingPackage packet, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for packet handlers with output.
/// </summary>
public abstract class PacketHandlerBase<TIncomingPackage, TOutgoingPackage> : IPacketHandler<TIncomingPackage, TOutgoingPackage>
    where TIncomingPackage : MqttPacket
    where TOutgoingPackage : MqttPacket
{
    public async Task<MqttPacket?> HandleAsync(IMqttConnection connection, MqttPacket packet, CancellationToken cancellationToken)
    {
        if (packet is not TIncomingPackage typedPacket)
        {
            throw new ArgumentException($"Expected {typeof(TIncomingPackage).Name}, got {packet.GetType().Name}", nameof(packet));
        }
        return await HandleAsync(connection, typedPacket, cancellationToken);
    }

    public abstract Task<TOutgoingPackage?> HandleAsync(IMqttConnection connection, TIncomingPackage packet, CancellationToken cancellationToken);
}

