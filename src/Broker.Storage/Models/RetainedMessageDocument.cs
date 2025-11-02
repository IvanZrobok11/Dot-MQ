using Broker.Core;

namespace Broker.Storage.Models;

// Retained Messages Models
public class RetainedMessageDocument
{
    public string Id { get; set; } = string.Empty; // Topic
    public string Topic { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    public QoSLevel QoS { get; set; }
    public DateTime StoredAt { get; set; }
}
