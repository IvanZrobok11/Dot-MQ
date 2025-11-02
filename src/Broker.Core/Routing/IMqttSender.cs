namespace Broker.Core.Routing;

/// <summary>
/// Interface for sending MQTT packets to a client.
/// </summary>
public interface IMqttSender
{
    /// <summary>
    /// Sends a packet to the client asynchronously.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SendAsync(MqttPacket packet, CancellationToken cancellationToken);
}

public interface IClient
{
    /// <summary>
    /// Gets the client identifier.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    bool IsConnected { get; }
}

public interface IMqttConnection : IMqttSender, IClient { }
