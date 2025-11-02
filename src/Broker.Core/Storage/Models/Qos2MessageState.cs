using Broker.Core.Packets;

namespace Broker.Core.Storage.Models;

public sealed class Qos2PendingState
{
    public string ClientId { get; init; } = default!;
    public ushort PacketId { get; init; }

    public MqttPublishPacket Packet { get; init; } = default!;
    public Qos2State State { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
