using Broker.Core;
using Broker.Core.Storage.Models;

namespace Broker.Storage.Models;

// Pending Messages Models
public class PendingMessageDocument
{
    public string Id { get; set; } = string.Empty; // {ClientId}_{PacketId}
    public string ClientId { get; set; } = string.Empty;
    public ushort PacketId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public QoSLevel QoS { get; set; }
    public bool Retain { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }

    // QoS 2 specific fields
    public Qos2State? Qos2State { get; set; }
}
