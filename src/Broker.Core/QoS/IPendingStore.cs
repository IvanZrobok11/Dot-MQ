using Broker.Core.Packets;
using Broker.Core.Storage.Models;

namespace Broker.Core.Qos;

public interface IPendingStore
{
    Task AddPendingAsync(string clientId, ushort packetId, MqttPublishPacket message, CancellationToken cancellationToken, Qos2State? state = null);
    Task RemovePendingAsync(string clientId, ushort packetId, CancellationToken cancellationToken);
    Task<Qos2PendingState?> GetQos2StateAsync(string clientId, ushort packetId, CancellationToken cancellationToken);
    Task RemoveQos2StateAsync(string clientId, ushort packetId, CancellationToken cancellationToken);
    Task<IEnumerable<PendingMessage>> GetPendingMessagesAsync(string clientId, CancellationToken cancellationToken);
    Task<IEnumerable<PendingMessage>> GetAllPendingMessagesAsync(CancellationToken cancellationToken);

}
