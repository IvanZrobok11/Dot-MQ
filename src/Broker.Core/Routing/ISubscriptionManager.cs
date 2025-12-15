using Broker.Core.Storage.Models;

namespace Broker.Core.Routing;

/// <summary>
/// Interface for managing client subscriptions.
/// </summary>
public interface ISubscriptionManager
{
    /// <summary>
    /// Adds a subscription for a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="topicFilter">Topic filter to subscribe to.</param>
    /// <param name="requestedQos">Requested QoS level.</param>
    /// <returns>The granted QoS level (may be downgraded from requested).</returns>
    QoSLevel Subscribe(string clientId, string topicFilter, QoSLevel requestedQos);

    /// <summary>
    /// Removes a subscription for a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="topicFilter">Topic filter to unsubscribe from.</param>
    /// <returns>True if subscription was removed, false if it didn't exist.</returns>
    bool Unsubscribe(string clientId, string topicFilter);

    /// <summary>
    /// Removes all subscriptions for a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    void UnsubscribeAll(string clientId);

    /// <summary>
    /// Gets all subscriptions for a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <returns>List of subscriptions for the client.</returns>
    IReadOnlyList<Subscription> GetSubscriptions(string clientId);

    /// <summary>
    /// Gets all subscribers for a topic name (matching topic filters).
    /// </summary>
    /// <param name="topicName">Topic name to find subscribers for.</param>
    /// <returns>List of subscriptions that match the topic.</returns>
    IReadOnlyList<Subscription> GetSubscribers(string topicName);

    /// <summary>
    /// Gets all subscribers that have a specific topic filter.
    /// </summary>
    /// <param name="topicFilter">Topic filter to find subscribers for.</param>
    /// <returns>List of subscriptions that have the exact topic filter.</returns>
    IReadOnlyList<Subscription> GetSubscribersByTopicFilter(string topicFilter);

    /// <summary>
    /// Gets all subscriptions across all clients.
    /// </summary>
    /// <returns>List of all subscriptions.</returns>
    IReadOnlyList<Subscription> GetAllSubscriptions();
}

