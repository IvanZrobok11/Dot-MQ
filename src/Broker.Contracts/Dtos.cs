namespace Broker.Contracts;

/// <summary>
/// DTO for client information.
/// </summary>
public class ClientInfoDto
{
    public string ClientId { get; set; } = string.Empty;
    public string? RemoteEndPoint { get; set; }
    public bool IsConnected { get; set; }
    public DateTime? LastActivity { get; set; }
    public bool HasWillMessage { get; set; }
    public bool CleanSession { get; set; }
    public int SubscriptionCount { get; set; }
}

/// <summary>
/// DTO for subscription information.
/// </summary>
public class SubscriptionDto
{
    public string ClientId { get; set; } = string.Empty;
    public string TopicFilter { get; set; } = string.Empty;
    public string GrantedQos { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for retained message information.
/// </summary>
public class RetainedMessageDto
{
    public string Topic { get; set; } = string.Empty;
    public string PayloadPreview { get; set; } = string.Empty;
    public int PayloadSize { get; set; }
    public string Qos { get; set; } = string.Empty;
    public DateTime RetainedAt { get; set; }
}

/// <summary>
/// DTO for pending message information.
/// </summary>
public class PendingMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string PayloadPreview { get; set; } = string.Empty;
    public int PayloadSize { get; set; }
    public string Qos { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// DTO for broker metrics.
/// </summary>
public class BrokerMetricsDto
{
    public int ConnectedClients { get; set; }
    public long TotalMessagesPublished { get; set; }
    public long TotalMessagesReceived { get; set; }
    public double MessagesPerSecond { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int RetainedMessages { get; set; }
    public int PendingMessages { get; set; }
}

/// <summary>
/// DTO for publishing a message.
/// </summary>
public class PublishMessageDto
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int Qos { get; set; }
    public bool Retain { get; set; }
}

/// <summary>
/// DTO for creating a subscription.
/// </summary>
public class CreateSubscriptionDto
{
    public string ClientId { get; set; } = string.Empty;
    public string TopicFilter { get; set; } = string.Empty;
    public int RequestedQos { get; set; }
}

/// <summary>
/// DTO for subscription result.
/// </summary>
public class SubscriptionResultDto
{
    public string ClientId { get; set; } = string.Empty;
    public string TopicFilter { get; set; } = string.Empty;
    public int GrantedQos { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

