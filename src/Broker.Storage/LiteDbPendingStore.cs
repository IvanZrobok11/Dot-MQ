using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Broker.Storage.Models;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Broker.Storage;

public class LiteDbPendingStore : IPendingStore, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<PendingMessageDocument> _pending;
    private readonly ILogger<LiteDbPendingStore> _logger;
    private readonly IBrokerMetricsService _metrics;

    public LiteDbPendingStore(LiteDbProvider liteDbProvider, ILogger<LiteDbPendingStore> logger, IBrokerMetricsService metrics)
    {
        _db = liteDbProvider.Database;
        _logger = logger;
        _metrics = metrics;

        _pending = _db.GetCollection<PendingMessageDocument>("pending");
        _pending.EnsureIndex(x => x.Id, unique: true);
        _pending.EnsureIndex(x => x.ClientId);
        _pending.EnsureIndex(x => x.CreatedAt);

        _logger.LogInformation("LiteDB Pending Store initialized");
        _metrics.UpdatePendingMessages(_pending.Count());
    }

    public async Task AddPendingAsync(string clientId, ushort packetId, MqttPublishPacket message, CancellationToken cancellationToken, Qos2State? state = null)
    {
        var doc = new PendingMessageDocument
        {
            Id = $"{clientId}_{packetId}",
            ClientId = clientId,
            PacketId = packetId,
            Topic = message.TopicName,
            Payload = message.Payload ?? Array.Empty<byte>(),
            QoS = message.QoS,
            Retain = message.Retain,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Qos2State = state
        };

        _pending.Upsert(doc);
        _metrics.UpdatePendingMessages(_pending.Count());
    }


    public async Task RemovePendingAsync(string clientId, ushort packetId, CancellationToken cancellationToken)
    {
        var id = $"{clientId}_{packetId}";
        _pending.Delete(id);

        _logger.LogDebug("Removed pending message for {ClientId}, PacketId {PacketId}",
            clientId, packetId);
        _metrics.UpdatePendingMessages(_pending.Count());
    }

    public Task<Qos2PendingState?> GetQos2StateAsync(string clientId, ushort packetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveQos2StateAsync(string clientId, ushort packetId, CancellationToken cancellationToken)
    {
        await RemovePendingAsync(clientId, packetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MqttPublishPacket?> GetPendingMessageAsync(string clientId, ushort packetId, CancellationToken cancellationToken)
    {
        var id = $"{clientId}_{packetId}";
        var doc = _pending.FindById(id);

        if (doc == null)
        {
            return null;
        }

        return MapToPublishPacket(doc);
    }

    public async Task<IEnumerable<PendingMessage>> GetAllPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var docs = _pending.Query()
                .OrderBy(x => x.CreatedAt)
                .ToList();

        return docs.Select(d => new PendingMessage
        {
            Id = d.Id,
            ClientId = d.ClientId,
            PacketId = d.PacketId,
            Topic = d.Topic,
            Payload = d.Payload,
            Qos = d.QoS,
            Retain = d.Retain,
            QueuedAt = d.CreatedAt,
            LastRetryAt = d.LastRetryAt,
            RetryCount = d.RetryCount
        });
    }

    public async Task<IEnumerable<PendingMessage>> GetPendingMessagesAsync(string clientId, CancellationToken cancellationToken)
    {
        var docs = _pending.Find(x => x.ClientId == clientId)
                .OrderBy(x => x.CreatedAt)
                .ToList();

        return docs.Select(d => new PendingMessage
        {
            Id = d.Id,
            ClientId = clientId,
            PacketId = d.PacketId,
            Topic = d.Topic,
            Payload = d.Payload,
            Qos = d.QoS,
            Retain = d.Retain,
            QueuedAt = d.CreatedAt,
            LastRetryAt = d.LastRetryAt,
            RetryCount = d.RetryCount
        });
    }

    public async Task ClearPendingMessagesAsync(string clientId, CancellationToken cancellationToken)
    {
        var count = _pending.DeleteMany(x => x.ClientId == clientId);
        _metrics.UpdatePendingMessages(_pending.Count());
    }

    public async Task UpdateRetryCountAsync(string clientId, ushort packetId, CancellationToken cancellationToken)
    {
        var id = $"{clientId}_{packetId}";
        var doc = _pending.FindById(id);

        if (doc != null)
        {
            doc.RetryCount++;
            doc.LastRetryAt = DateTime.UtcNow;
            _pending.Update(doc);

            _logger.LogDebug("Updated retry count for {ClientId}, PacketId {PacketId} to {Count}",
                clientId, packetId, doc.RetryCount);
        }
    }

    public async Task<int> GetPendingCountAsync(string clientId, CancellationToken cancellationToken)
    {
        return _pending.Count(x => x.ClientId == clientId);
    }

    private static MqttPublishPacket MapToPublishPacket(PendingMessageDocument doc)
    {
        return new MqttPublishPacket
        {
            TopicName = doc.Topic,
            Payload = doc.Payload,
            QoS = doc.QoS,
            Retain = doc.Retain,
            PacketId = doc.PacketId
        };
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
