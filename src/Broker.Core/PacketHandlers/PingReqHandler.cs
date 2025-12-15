using Broker.Core.Packets;
using Broker.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Broker.Core.PacketHandlers;

/// <summary>
/// Handles MQTT PINGREQ packets by replying with PINGRESP.
/// </summary>
public class PingReqHandler(
    ILogger<PingReqHandler> logger,
    IBrokerMetricsService metrics)
    : PacketHandlerBase<MqttPingReqPacket, MqttPingRespPacket>
{
    public override Task<MqttPingRespPacket?> HandleAsync(
        IMqttConnection connection,
        MqttPingReqPacket packet,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("PINGREQ received from {ClientId}", connection.ClientId);
        metrics.RecordMessageReceived();

        // Respond with PINGRESP to keep the connection alive
        var response = new MqttPingRespPacket();
        return Task.FromResult<MqttPingRespPacket?>(response);
    }
}


