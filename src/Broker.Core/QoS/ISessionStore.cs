using Broker.Core.Storage.Models;

namespace Broker.Core.Qos;

public interface ISessionStore
{
    Task<MqttSession?> GetSessionAsync(string clientId, CancellationToken ct);
    Task SaveSessionAsync(MqttSession session, CancellationToken ct);
    Task DeleteSessionAsync(string clientId, CancellationToken ct);
    //Task<IEnumerable<PendingMessage>> GetPendingMessagesAsync(string clientId, CancellationToken ct);
}
