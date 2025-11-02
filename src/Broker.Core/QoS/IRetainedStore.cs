using Broker.Core.Packets;
using Broker.Core.Storage.Models;

namespace Broker.Core.Qos;

public interface IRetainedStore
{
    Task SaveAsync(MqttPublishPacket message, CancellationToken cancellationToken);
    Task DeleteAsync(string topic, CancellationToken cancellationToken);
    Task<IEnumerable<RetainedMessage>> MatchAsync(string topicFilter, CancellationToken cancellationToken);
    Task<List<MqttPublishPacket>?> GetAllRetainedMessageAsync(CancellationToken cancellationToken);
}