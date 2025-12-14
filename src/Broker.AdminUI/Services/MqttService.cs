using MQTTnet;
using MQTTnet.Client;

namespace Broker.AdminUI.Services;

/// <summary>
/// MQTT service implementation using MQTTnet.
/// </summary>
public class MqttService : IMqttService, IAsyncDisposable
{
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly ILogger<MqttService> _logger;
    private IMqttClient? _mqttClient;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public MqttService(string brokerHost, int brokerPort, ILogger<MqttService> logger)
    {
        _brokerHost = brokerHost;
        _brokerPort = brokerPort;
        _logger = logger;
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_mqttClient?.IsConnected == true)
        {
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_mqttClient?.IsConnected == true)
            {
                return;
            }

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerHost, _brokerPort)
                .WithClientId($"AdminUI_{Guid.NewGuid():N}")
                .WithCleanSession()
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            var connectResult = await _mqttClient.ConnectAsync(clientOptions, cancellationToken);
            
            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("Failed to connect to MQTT broker: {ResultCode}", connectResult.ResultCode);
                throw new InvalidOperationException($"Failed to connect to MQTT broker: {connectResult.ResultCode}");
            }

            _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", _brokerHost, _brokerPort);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            var args = new MessageReceivedEventArgs
            {
                Topic = e.ApplicationMessage.Topic ?? string.Empty,
                Payload = payload,
                Qos = (int)e.ApplicationMessage.QualityOfServiceLevel,
                Retain = e.ApplicationMessage.Retain,
                ReceivedAt = DateTime.UtcNow
            };

            MessageReceived?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received message");
        }

        return Task.CompletedTask;
    }

    public async Task<bool> PublishAsync(string topic, string payload, int qos = 0, bool retain = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_mqttClient == null)
            {
                return false;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .WithRetainFlag(retain)
                .Build();

            var result = await _mqttClient.PublishAsync(message, cancellationToken);
            
            if (result.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                _logger.LogInformation("Published message to topic {Topic} with QoS {Qos}", topic, qos);
                return true;
            }

            _logger.LogWarning("Failed to publish message: {ReasonCode}", result.ReasonCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to topic {Topic}", topic);
            return false;
        }
    }

    public async Task<bool> SubscribeAsync(string topicFilter, int qos = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_mqttClient == null)
            {
                return false;
            }

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter, (MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .Build();

            var result = await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            
            if (result?.Items != null && result.Items.Any() && (result.Items.First().ResultCode == MqttClientSubscribeResultCode.GrantedQoS0 ||
                result.Items.First().ResultCode == MqttClientSubscribeResultCode.GrantedQoS1 ||
                result.Items.First().ResultCode == MqttClientSubscribeResultCode.GrantedQoS2))
            {
                _logger.LogInformation("Subscribed to topic filter {TopicFilter} with QoS {Qos}", topicFilter, qos);
                return true;
            }

            _logger.LogWarning("Failed to subscribe to topic filter {TopicFilter}: {ResultCode}", topicFilter, result?.Items.First().ResultCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to topic filter {TopicFilter}", topicFilter);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                return false;
            }

            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build();

            var result = await _mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken);
            
            if (result != null && result.Items.Any() && result.Items.First().ResultCode == MqttClientUnsubscribeResultCode.Success)
            {
                _logger.LogInformation("Unsubscribed from topic filter {TopicFilter}", topicFilter);
                return true;
            }

            _logger.LogWarning("Failed to unsubscribe from topic filter {TopicFilter}", topicFilter);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from topic filter {TopicFilter}", topicFilter);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_mqttClient?.IsConnected == true)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting MQTT client");
            }
        }

        _mqttClient?.Dispose();
        _connectionLock.Dispose();
        _disposed = true;
    }
}

