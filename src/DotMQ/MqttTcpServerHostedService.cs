using Broker.Core;
using Broker.Frontend;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace DotMQ;

public sealed class MqttTcpServerHostedService(
    IMqttTcpServer mqttTcpServer,
    IOptions<BrokerOptions> options,
    ILogger<MqttTcpServerHostedService> logger) : BackgroundService
{
    private readonly BrokerOptions _options = options.Value;
    private TcpListener? _listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _options.TcpPort);
        _listener.Start(_options.Backlog);
        logger.LogInformation("Listening MQTT TCP on port {Port}", _options.TcpPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Respect maximum connections at acceptance point
                if (mqttTcpServer.ActiveConnections >= _options.MaxConnections)
                {
                    logger.LogWarning("Max connections reached ({MaxConnections}), throttling accept", _options.MaxConnections);
                    await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
                    continue;
                }

                TcpClient client;
                try
                {
                    // new client is new TCP connection
                    client = await _listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                // Push client into server's queue (may be bounded)
                var enqueued = await mqttTcpServer.EnqueueTcpClientAsync(client, stoppingToken);
                if (!enqueued)
                {
                    logger.LogWarning("Dropping incoming client");
                    try
                    {
                        client.Close();
                        client.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while closing TCP client");
                    }
                }
            }
        }
        finally
        {
            _listener?.Stop();
            logger.LogInformation("Stopped TCP listener");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping MQTT TCP server hosted service...");
        _listener?.Stop();
        await mqttTcpServer.CloseAllConnectionsAsync();
        await base.StopAsync(cancellationToken);
    }
}
