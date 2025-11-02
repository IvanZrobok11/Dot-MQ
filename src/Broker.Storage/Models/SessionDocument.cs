using Broker.Core;
using LiteDB;

namespace Broker.Storage.Models;

// Session Store Models
public class SessionDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty; // ClientId
    public bool CleanSession { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastConnectedAt { get; set; }
    public QoSLevel? WillQoS { get; set; }
    public bool? WillRetain { get; set; }
    public string? WillTopic { get; set; }
    public byte[]? WillPayload { get; set; }
}
