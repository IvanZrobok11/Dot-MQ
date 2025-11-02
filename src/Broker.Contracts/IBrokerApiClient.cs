using Refit;

namespace Broker.Contracts;

/// <summary>
/// Refit interface for the broker API client.
/// </summary>
public interface IBrokerApiClient
{
    [Get("/api/broker/metrics")]
    Task<BrokerMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default);

    [Get("/api/broker/clients")]
    Task<IEnumerable<ClientInfoDto>> GetActiveClientsAsync(CancellationToken cancellationToken = default);

    [Get("/api/broker/clients/{clientId}")]
    Task<ClientInfoDto> GetClientInfoAsync(string clientId, CancellationToken cancellationToken = default);

    [Post("/api/broker/clients/{clientId}/disconnect")]
    Task DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default);

    [Get("/api/broker/subscriptions")]
    Task<IEnumerable<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    [Get("/api/broker/subscriptions/client/{clientId}")]
    Task<IEnumerable<SubscriptionDto>> GetClientSubscriptionsAsync(string clientId, CancellationToken cancellationToken = default);

    [Get("/api/broker/messages/retained")]
    Task<IEnumerable<RetainedMessageDto>> GetRetainedMessagesAsync(CancellationToken cancellationToken = default);

    [Delete("/api/broker/messages/retained/{topic}")]
    Task DeleteRetainedMessageAsync(string topic, CancellationToken cancellationToken = default);

    [Get("/api/broker/messages/pending")]
    Task<IEnumerable<PendingMessageDto>> GetPendingMessagesAsync(CancellationToken cancellationToken = default);

    [Get("/api/broker/messages/pending/client/{clientId}")]
    Task<IEnumerable<PendingMessageDto>> GetClientPendingMessagesAsync(string clientId, CancellationToken cancellationToken = default);
}

