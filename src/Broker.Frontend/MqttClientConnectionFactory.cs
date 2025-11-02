using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Broker.Frontend;

public static class ConnectionExtension
{
    public static IServiceCollection AddMqttClientConnectionFactory(this IServiceCollection services)
    {
        return services.AddScoped<MqttClientConnectionFactory>();
    }
}

public class MqttClientConnectionFactory(
        PacketDispatcher dispatcher,
        IMqttTcpServer server,
        ILogger<MqttClientConnection> logger)
{
    public MqttClientConnection CreateConnection(TcpClient tcpClient)
    {
        return new MqttClientConnection(
            tcpClient,
            dispatcher,
            server,
            logger);
    }
}
