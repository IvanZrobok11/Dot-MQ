namespace Broker.Storage.Models;

//// Configuration
//public class LiteDbStorageOptions
//{
//    public string DatabasePath { get; set; } = "mqtt_broker.db";
//    public bool EnableCache { get; set; } = true;
//    public int CacheExpirationMinutes { get; set; } = 5;
//}

//// Session Store Models
//public class SessionDocument
//{
//    public string Id { get; set; } = string.Empty; // ClientId
//    public bool CleanSession { get; set; }
//    public DateTime CreatedAt { get; set; }
//    public DateTime LastConnectedAt { get; set; }
//    public int WillQoS { get; set; }
//    public bool WillRetain { get; set; }
//    public string? WillTopic { get; set; }
//    public byte[]? WillPayload { get; set; }
//}

//// Pending Messages Models
//public class PendingMessageDocument
//{
//    public string Id { get; set; } = string.Empty; // {ClientId}_{PacketId}
//    public string ClientId { get; set; } = string.Empty;
//    public ushort PacketId { get; set; }
//    public string Topic { get; set; } = string.Empty;
//    public byte[] Payload { get; set; } = Array.Empty<byte>();
//    public byte QoS { get; set; }
//    public bool Retain { get; set; }
//    public DateTime CreatedAt { get; set; }
//    public int RetryCount { get; set; }
//    public DateTime? LastRetryAt { get; set; }
//}

//// Retained Messages Models
//public class RetainedMessageDocument
//{
//    public string Id { get; set; } = string.Empty; // Topic
//    public string Topic { get; set; } = string.Empty;
//    public byte[] Payload { get; set; } = Array.Empty<byte>();
//    public byte QoS { get; set; }
//    public DateTime StoredAt { get; set; }
//}