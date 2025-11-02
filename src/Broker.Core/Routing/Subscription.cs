namespace Broker.Core.Routing;

/// <summary>
/// Represents a client subscription to a topic filter.
/// </summary>
public class Subscription
{
    /// <summary>
    /// Client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Topic filter (may contain wildcards).
    /// </summary>
    public string TopicFilter { get; set; } = string.Empty;

    /// <summary>
    /// Granted QoS level for this subscription.
    /// </summary>
    public QoSLevel GrantedQos { get; set; }

    /// <summary>
    /// Timestamp when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

