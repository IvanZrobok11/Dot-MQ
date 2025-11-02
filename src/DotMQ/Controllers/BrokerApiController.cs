using Broker.Contracts;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Frontend;
using Microsoft.AspNetCore.Mvc;

namespace DotMQ.Controllers;

/// <summary>
/// API controller for exposing broker data to the Admin UI.
/// </summary>
[ApiController]
[Route("api/broker")]
public class BrokerApiController(
    IMqttTcpServer mqttServer,
    IRetainedStore retainedStore,
    ISessionStore sessionStore,
    IPendingStore pendingStore,
    ISubscriptionManager subscriptionManager,
    IBrokerMetricsService metricsService,
    ILogger<BrokerApiController> logger) : ControllerBase
{
    [HttpGet("metrics")]
    public ActionResult<BrokerMetricsDto> GetMetrics()
    {
        return new BrokerMetricsDto
        {
            ConnectedClients = metricsService.ConnectedClients,
            TotalMessagesPublished = metricsService.TotalMessagesPublished,
            TotalMessagesReceived = metricsService.TotalMessagesReceived,
            MessagesPerSecond = metricsService.MessagesPerSecond,
            ActiveSubscriptions = metricsService.ActiveSubscriptions,
            RetainedMessages = metricsService.RetainedMessages,
            PendingMessages = metricsService.PendingMessages
        };
    }

    [HttpGet("clients")]
    public async Task<ActionResult<IEnumerable<ClientInfoDto>>> GetActiveClients(CancellationToken cancellationToken)
    {
        var connections = MqttTcpServer.ConnectionsByClientId.Where(s => s.Value.IsConnected).ToDictionary();
        var clients = new List<ClientInfoDto>();

        foreach (var kvp in connections)
        {
            var clientId = kvp.Key;
            var connection = kvp.Value;

            var session = await sessionStore.GetSessionAsync(clientId, cancellationToken);
            var subscriptions = subscriptionManager.GetSubscriptions(clientId);

            clients.Add(new ClientInfoDto
            {
                ClientId = clientId,
                IsConnected = connection.IsConnected,
                CleanSession = session?.CleanSession ?? false,
                HasWillMessage = session?.WillMessage != null,
                SubscriptionCount = subscriptions.Count
            });
        }

        return clients;
    }

    [HttpGet("clients/{clientId}")]
    public async Task<ActionResult<ClientInfoDto>> GetClientInfo(string clientId, CancellationToken cancellationToken)
    {
        var connection = await mqttServer.GetConnectionByClientIdAsync(clientId);
        if (connection == null || !connection.IsConnected)
        {
            return NotFound();
        }

        var session = await sessionStore.GetSessionAsync(clientId, cancellationToken);
        var subscriptions = subscriptionManager.GetSubscriptions(clientId);

        return new ClientInfoDto
        {
            ClientId = clientId,
            IsConnected = connection.IsConnected,
            CleanSession = session?.CleanSession ?? false,
            HasWillMessage = session?.WillMessage != null,
            SubscriptionCount = subscriptions.Count
        };
    }

    [HttpPost("clients/{clientId}/disconnect")]
    public async Task<IActionResult> DisconnectClient(string clientId, CancellationToken cancellationToken)
    {
        var connection = await mqttServer.GetConnectionByClientIdAsync(clientId);
        if (connection == null)
        {
            return NotFound();
        }

        connection.Dispose();
        logger.LogInformation("Client {ClientId} disconnected via API", clientId);
        return Ok();
    }

    [HttpGet("subscriptions")]
    public ActionResult<IEnumerable<SubscriptionDto>> GetSubscriptions()
    {
        var subscriptions = subscriptionManager.GetAllSubscriptions();
        var dtos = subscriptions.Select(s => new SubscriptionDto
        {
            ClientId = s.ClientId,
            TopicFilter = s.TopicFilter,
            GrantedQos = s.GrantedQos.ToString(),
            CreatedAt = s.CreatedAt
        });

        return dtos.ToList();
    }

    [HttpGet("subscriptions/client/{clientId}")]
    public ActionResult<IEnumerable<SubscriptionDto>> GetClientSubscriptions(string clientId)
    {
        var subscriptions = subscriptionManager.GetSubscriptions(clientId);
        var dtos = subscriptions.Select(s => new SubscriptionDto
        {
            ClientId = s.ClientId,
            TopicFilter = s.TopicFilter,
            GrantedQos = s.GrantedQos.ToString(),
            CreatedAt = s.CreatedAt
        });

        return dtos.ToList();
    }

    [HttpGet("messages/retained")]
    public async Task<ActionResult<IEnumerable<RetainedMessageDto>>> GetRetainedMessages(CancellationToken cancellationToken)
    {
        var messages = await retainedStore.GetAllRetainedMessageAsync(cancellationToken) ?? new();
        var dtos = messages.Select(m => new RetainedMessageDto
        {
            Topic = m.TopicName,
            PayloadPreview = GetPayloadPreview(m.Payload),
            PayloadSize = m.Payload?.Length ?? 0,
            Qos = m.QoS.ToString(),
            RetainedAt = DateTime.MinValue // TODO
        });

        return dtos.ToList();
    }

    [HttpDelete("messages/retained/{topic}")]
    public async Task<IActionResult> DeleteRetainedMessage(string topic, CancellationToken cancellationToken)
    {
        await retainedStore.DeleteAsync(topic, cancellationToken);
        logger.LogInformation("Retained message for topic {Topic} deleted via API", topic);
        return Ok();
    }

    [HttpGet("messages/pending")]
    public async Task<ActionResult<IEnumerable<PendingMessageDto>>> GetPendingMessages(CancellationToken cancellationToken)
    {
        var messages = await pendingStore.GetAllPendingMessagesAsync(cancellationToken);
        var dtos = messages.Select(m => new PendingMessageDto
        {
            Id = m.Id,
            ClientId = m.ClientId,
            Topic = m.Topic,
            PayloadPreview = GetPayloadPreview(m.Payload),
            PayloadSize = m.Payload?.Length ?? 0,
            Qos = m.Qos.ToString(),
            QueuedAt = m.QueuedAt,
            RetryCount = m.RetryCount
        });

        return dtos.ToList();
    }

    [HttpGet("messages/pending/client/{clientId}")]
    public async Task<ActionResult<IEnumerable<PendingMessageDto>>> GetClientPendingMessages(string clientId, CancellationToken cancellationToken)
    {
        var messages = await pendingStore.GetPendingMessagesAsync(clientId, cancellationToken);
        var dtos = messages.Select(m => new PendingMessageDto
        {
            Id = m.Id,
            ClientId = m.ClientId,
            Topic = m.Topic,
            PayloadPreview = GetPayloadPreview(m.Payload),
            PayloadSize = m.Payload?.Length ?? 0,
            Qos = m.Qos.ToString(),
            QueuedAt = m.QueuedAt,
            RetryCount = m.RetryCount
        });

        return dtos.ToList();
    }

    private static string GetPayloadPreview(byte[]? payload, int maxLength = 100)
    {
        if (payload == null || payload.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            var text = System.Text.Encoding.UTF8.GetString(payload);
            if (text.Length > maxLength)
            {
                return text.Substring(0, maxLength) + "...";
            }
            return text;
        }
        catch
        {
            var hex = BitConverter.ToString(payload.Take(maxLength / 2).ToArray());
            if (payload.Length > maxLength / 2)
            {
                hex += "...";
            }
            return hex;
        }
    }
}

