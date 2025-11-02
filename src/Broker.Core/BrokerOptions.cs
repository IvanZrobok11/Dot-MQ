namespace Broker.Core;

/// <summary>
/// Configuration options for the MQTT broker.
/// </summary>
public class BrokerOptions
{
    /// <summary>
    /// TCP port for MQTT connections. Default: 1883
    /// </summary>
    public int TcpPort { get; set; } = 1883;

    /// <summary>
    /// WebSocket port for browser-based MQTT clients. Default: 8080
    /// </summary>
    public int WebSocketPort { get; set; } = 8080;

    /// <summary>
    /// Maximum number of concurrent connections. Default: 1000
    /// </summary>
    public int MaxConnections { get; set; } = 1000;

    /// <summary>
    /// Keep-alive timeout in seconds. Default: 60
    /// </summary>
    public int KeepAliveTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable WebSocket support. Default: true
    /// </summary>
    public bool EnableWebSocket { get; set; } = true;
    public int AcceptQueueCapacity { get; set; } = 100;
    public int AcceptWorkerCount { get; set; } = 100;
    public int MaxPacketSize { get; set; } = 4048;
    public int Backlog { get; set; } = 100;
}

