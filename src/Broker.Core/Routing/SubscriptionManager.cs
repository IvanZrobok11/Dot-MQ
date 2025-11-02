using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Broker.Core.Routing;

/// <summary>
/// Thread-safe subscription manager for MQTT broker.
/// </summary>
public class SubscriptionManager(ILogger<SubscriptionManager> logger) : ISubscriptionManager
{
    // Key: ClientId, Value: Dictionary of TopicFilter -> Subscription
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Subscription>> _subscriptions = new();

    public QoSLevel Subscribe(string clientId, string topicFilter, QoSLevel requestedQos)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
        }

        if (string.IsNullOrEmpty(topicFilter))
        {
            throw new ArgumentException("Topic filter cannot be null or empty", nameof(topicFilter));
        }

        if (!TopicMatcher.IsValidTopicFilter(topicFilter))
        {
            throw new ArgumentException("Invalid topic filter", nameof(topicFilter));
        }

        //TODO: Grant QoS (for now, grant the requested QoS - can be downgraded later based on client capabilities)
        QoSLevel grantedQos = requestedQos;

        var clientSubscriptions = _subscriptions.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, Subscription>());

        var subscription = new Subscription
        {
            ClientId = clientId,
            TopicFilter = topicFilter,
            GrantedQos = grantedQos,
            CreatedAt = DateTime.UtcNow
        };

        clientSubscriptions.AddOrUpdate(topicFilter, subscription, (_, _) => subscription);

        return grantedQos;
    }

    public bool Unsubscribe(string clientId, string topicFilter)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(topicFilter))
        {
            return false;
        }

        if (!_subscriptions.TryGetValue(clientId, out var clientSubscriptions))
        {
            return false;
        }

        bool removed = clientSubscriptions.TryRemove(topicFilter, out _);

        // Clean up empty client subscription dictionaries
        if (clientSubscriptions.IsEmpty)
        {
            _subscriptions.TryRemove(clientId, out _);
        }

        return removed;
    }

    public void UnsubscribeAll(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        _subscriptions.TryRemove(clientId, out _);
    }

    public IReadOnlyList<Subscription> GetSubscriptions(string clientId)
    {
        if (!_subscriptions.TryGetValue(clientId, out var clientSubscriptions))
        {
            return Array.Empty<Subscription>();
        }

        return clientSubscriptions.Values.ToList();
    }

    public IReadOnlyList<Subscription> GetSubscribers(string topicName)
    {
        if (string.IsNullOrEmpty(topicName) || !TopicMatcher.IsValidTopicName(topicName))
        {
            return [];
        }

        return _subscriptions.Values
            .SelectMany(clientSubscriptions => clientSubscriptions.Values
                .Where(subscription => TopicMatcher.Matches(subscription.TopicFilter, topicName)))
            .ToList();
    }

    public IReadOnlyList<Subscription> GetAllSubscriptions()
    {
        return _subscriptions.Values.SelectMany(s => s.Values).ToList();
    }
}

