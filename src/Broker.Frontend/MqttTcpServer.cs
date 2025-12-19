// Broker.Frontend/MqttTcpServer.cs
using Broker.Core;
using Broker.Core.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Broker.Frontend;

/// <summary>
/// Interface for the MQTT TCP server that manages client connections.
/// </summary>
/// // Server that holds queue and worker pool
public interface IMqttTcpServer
{
    int ActiveConnections { get; }
    Task<bool> EnqueueTcpClientAsync(TcpClient client, CancellationToken cancellationToken);
    Task RegisterClientConnectionAsync(string clientId, MqttClientConnection connection);
    Task UnregisterClientConnectionAsync(string clientId);
    Task CloseAllConnectionsAsync();
    Task<MqttClientConnection?> GetConnectionByClientIdAsync(string clientId);
}
public sealed class MqttTcpServer : IMqttTcpServer, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly BrokerOptions _options;
    private readonly ILogger<MqttTcpServer> _logger;
    private readonly IBrokerMetricsService _metrics;

    private readonly Channel<TcpClient> _acceptChannel;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Task> _workers = new();

    public MqttTcpServer(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<BrokerOptions> options,
        ILogger<MqttTcpServer> logger,
        IBrokerMetricsService metrics)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;

        var capacity = Math.Max(16, _options.AcceptQueueCapacity);
        var opts = new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait };
        _acceptChannel = Channel.CreateBounded<TcpClient>(opts);
        _cancellationTokenSource = new();
        CreateWorkers();
    }

    public static readonly ConcurrentDictionary<string, MqttClientConnection> ConnectionsByClientId = new();
    public int ActiveConnections => ConnectionsByClientId.Count;

    public async Task<bool> EnqueueTcpClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
            await _acceptChannel.Writer.WriteAsync(client, linked.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enqueue failed");
            return false;
        }
    }

    private void CreateWorkers()
    {
        for (int i = 0; i < Math.Max(1, _options.AcceptWorkerCount); i++)
        {
            _workers.Add(Task.Run(() => AcceptWorkerLoopAsync(_cancellationTokenSource.Token)));
        }
    }

    private async Task AcceptWorkerLoopAsync(CancellationToken cancellationToken)
    {
        await foreach (var tcpClient in _acceptChannel.Reader.ReadAllAsync(cancellationToken))
        {
            _ = HandleConnectionSafeAsync(tcpClient, cancellationToken);
        }
    }

    private async Task HandleConnectionSafeAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        MqttClientConnection? mqttConnection = null;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            try
            {
                var options = new BrokerOptions
                {
                    MaxPacketSize = _options.MaxPacketSize,
                };
                var factory = scope.ServiceProvider.GetRequiredService<MqttClientConnectionFactory>();
                mqttConnection = factory.CreateConnection(tcpClient);

                await mqttConnection.ProcessAsync(cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled connection error {ClientId}", mqttConnection?.ClientId);
            }
            finally
            {
                if (mqttConnection != null && !string.IsNullOrEmpty(mqttConnection.ClientId))
                {
                    ConnectionsByClientId.TryRemove(mqttConnection.ClientId, out _);
                    _metrics.UpdateConnectedClients(ConnectionsByClientId.Count);
                }
                try
                {
                    tcpClient.Close();
                }
                catch
                {
                }
                mqttConnection?.Dispose();
                _logger.LogDebug("Connection {ConnectionId} closed", mqttConnection?.ClientId);
            }
        }
    }

    public Task RegisterClientConnectionAsync(string clientId, MqttClientConnection connection)
    {
        if (string.IsNullOrEmpty(clientId)) return Task.CompletedTask;

        if (ConnectionsByClientId.TryGetValue(clientId, out var old))
        {
            _logger.LogWarning("Duplicate CONNECT for {ClientId}. Replacing existing connection.", clientId);
            try { old.RequestDisconnect("Session taken over"); } catch { }
        }
        ConnectionsByClientId.AddOrUpdate(clientId, connection, (_, _) => connection);
        _metrics.UpdateConnectedClients(ConnectionsByClientId.Count);
        return Task.CompletedTask;
    }

    public Task UnregisterClientConnectionAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId)) return Task.CompletedTask;
        ConnectionsByClientId.TryRemove(clientId, out _);
        _metrics.UpdateConnectedClients(ConnectionsByClientId.Count);
        return Task.CompletedTask;
    }

    public async Task CloseAllConnectionsAsync()
    {
        _logger.LogInformation("Closing all connections...");
        _cancellationTokenSource.Cancel();
        _acceptChannel.Writer.Complete();

        try { await Task.WhenAll(_workers.ToArray()).WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        var list = ConnectionsByClientId.Values.ToArray();
        foreach (var c in list) { try { c.RequestDisconnect("Server shutdown"); } catch { } }
        await Task.Delay(200);
        ConnectionsByClientId.Clear();
        _metrics.UpdateConnectedClients(ConnectionsByClientId.Count);
        _logger.LogInformation("All connections closed");
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public async Task<MqttClientConnection?> GetConnectionByClientIdAsync(string clientId)
    {
        _ = ConnectionsByClientId.TryGetValue(clientId, out var connection);
        return connection;
    }
}
