using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

// Get configuration values
var brokerHost = configuration["Broker:Host"] ?? "localhost";
var brokerPort = configuration.GetValue<int>("Broker:Port", 1883);
var clientId = configuration["Subscriber:ClientId"];
var topics = configuration.GetSection("Subscriber:Topics").Get<string[]>() ?? Array.Empty<string>();
var qos = configuration.GetValue<int>("Subscriber:Qos", 0);
var cleanSession = configuration.GetValue<bool>("Subscriber:CleanSession", true);
var keepAliveSeconds = configuration.GetValue("Subscriber:KeepAliveSeconds", 60);

if (topics.Length == 0)
{
    logger.LogError("No topics configured for subscription. Please add topics in appsettings.json");
    return;
}

logger.LogInformation("Starting MQTT Subscriber...");
logger.LogInformation("Broker: {Host}:{Port}", brokerHost, brokerPort);
logger.LogInformation("Client ID: {ClientId}", clientId);
logger.LogInformation("Topics to subscribe: {Topics}", string.Join(", ", topics));

// Create MQTT factory and client
var mqttFactory = new MqttFactory();
using var mqttClient = mqttFactory.CreateMqttClient();

// Configure message received handler
mqttClient.ApplicationMessageReceivedAsync += e =>
{
    var topic = e.ApplicationMessage.Topic;
    var payload = e.ApplicationMessage.ConvertPayloadToString();
    var qosLevel = e.ApplicationMessage.QualityOfServiceLevel;
    var retain = e.ApplicationMessage.Retain;

    logger.LogInformation(
        "Received message - Topic: {Topic}, QoS: {Qos}, Retain: {Retain}, Payload: {Payload}",
        topic, qosLevel, retain, payload);

    return Task.CompletedTask;
};

// Configure connection options
var clientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(brokerHost, brokerPort)
    .WithClientId(clientId)
    .WithCleanSession(cleanSession)
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepAliveSeconds))
    .Build();

try
{
    // Connect to broker
    logger.LogInformation("Connecting to broker...");
    var connectResult = await mqttClient.ConnectAsync(clientOptions);

    if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
    {
        logger.LogError("Failed to connect to broker. Result: {ResultCode}", connectResult.ResultCode);
        return;
    }

    logger.LogInformation("Successfully connected to broker");

    // Subscribe to topics
    var qosLevel = (MQTTnet.Protocol.MqttQualityOfServiceLevel)Math.Clamp(qos, 0, 2);
    var subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder();

    foreach (var topic in topics)
    {
        subscribeOptions.WithTopicFilter(f => f
            .WithTopic(topic)
            .WithQualityOfServiceLevel(qosLevel));

        logger.LogInformation("Subscribing to topic: {Topic} with QoS {Qos}", topic, qosLevel);
    }

    var subscribeResult = await mqttClient.SubscribeAsync(subscribeOptions.Build());

    foreach (var item in subscribeResult.Items)
    {
        if (item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS0 ||
            item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS1 ||
            item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS2)
        {
            logger.LogInformation("Successfully subscribed to topic with QoS: {Qos}", item.ResultCode);
        }
        else
        {
            logger.LogWarning("Failed to subscribe. Result: {ResultCode}", item.ResultCode);
        }
    }

    logger.LogInformation("Subscriber is running. Press Ctrl+C to exit.");

    // Keep the application running
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        cancellationTokenSource.Cancel();
        logger.LogInformation("Shutting down...");
    };

    // Wait for cancellation
    try
    {
        await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when cancellation is requested
    }

    // Disconnect
    logger.LogInformation("Disconnecting from broker...");
    await mqttClient.DisconnectAsync();
    logger.LogInformation("Disconnected successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while running the subscriber");
}
