using Broker.Core.Packets;
using Broker.Core.Qos;
using Broker.Core.Routing;
using Broker.Core.Storage.Models;
using Broker.Storage.Models;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Broker.Storage;

public class LiteDbRetainedStore : IRetainedStore, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<RetainedMessageDocument> _retained;
    private readonly ILogger<LiteDbRetainedStore> _logger;
    private readonly IBrokerMetricsService _metrics;

    public LiteDbRetainedStore(LiteDbProvider liteDbProvider, ILogger<LiteDbRetainedStore> logger, IBrokerMetricsService metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _db = liteDbProvider.Database;
        _retained = _db.GetCollection<RetainedMessageDocument>("retained");
        _retained.EnsureIndex(x => x.Id, unique: true);
        _retained.EnsureIndex(x => x.Topic);

        _logger.LogInformation("LiteDB Retained Store initialized");
        _metrics.UpdateRetainedMessages(_retained.Count());
    }

    public async Task SaveAsync(MqttPublishPacket message, CancellationToken cancellationToken)
    {
        var topic = message.TopicName;
        // Empty payload means delete retained message
        if (message.Payload == null || message.Payload.Length == 0)
        {
            await DeleteAsync(topic, cancellationToken);
            return;
        }

        var doc = new RetainedMessageDocument
        {
            Id = topic,
            Topic = topic,
            Payload = message.Payload,
            QoS = message.QoS,
            StoredAt = DateTime.UtcNow
        };

        _retained.Upsert(doc);

        _logger.LogDebug("Set retained message for topic {Topic}", topic);
        _metrics.UpdateRetainedMessages(_retained.Count());
    }

    public async Task<MqttPublishPacket?> GetRetainedMessageAsync(string topic, CancellationToken cancellationToken)
    {
        var doc = _retained.FindById(topic);
        if (doc == null)
        {
            return null;
        }

        return MapToPublishPacket(doc);
    }

    public async Task<List<MqttPublishPacket>?> GetAllRetainedMessageAsync(CancellationToken cancellationToken)
    {
        var docs = _retained.Query().ToList();
        if (docs == null)
        {
            return null;
        }

        return docs.Select(doc => MapToPublishPacket(doc)).ToList();
    }

    public async Task<IEnumerable<RetainedMessage>> MatchAsync(string topicFilter, CancellationToken cancellationToken)
    {
        var allDocs = _retained.FindAll().ToList();
        var matching = new List<RetainedMessage>();

        foreach (var doc in allDocs)
        {
            if (TopicMatches(doc.Topic, topicFilter))
            {
                matching.Add(new RetainedMessage
                {
                    Topic = doc.Topic,
                    Payload = doc.Payload,
                    Qos = doc.QoS,
                    RetainedAt = doc.StoredAt
                });
            }
        }

        _logger.LogDebug("Found {Count} retained messages matching {Filter}",
            matching.Count, topicFilter);

        return matching;
    }

    public async Task DeleteAsync(string topic, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        try
        {
            //TODO: potential bug
            _retained.Delete(topic);

            _logger.LogDebug("Deleted retained message for topic {Topic}", topic);
            _metrics.UpdateRetainedMessages(_retained.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting retained message for {Topic}", topic);
            throw;
        }
    }

    public async Task<int> GetRetainedCountAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        try
        {
            return _retained.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting retained count");
            throw;
        }
    }

    public async Task ClearAllRetainedAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        try
        {
            var count = _retained.DeleteAll();

            _logger.LogInformation("Cleared all {Count} retained messages", count);
            _metrics.UpdateRetainedMessages(_retained.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all retained messages");
            throw;
        }
    }

    private static MqttPublishPacket MapToPublishPacket(RetainedMessageDocument doc)
    {
        return new MqttPublishPacket
        {
            TopicName = doc.Topic,
            Payload = doc.Payload,
            QoS = doc.QoS,
            Retain = true
        };
    }

    private static bool TopicMatches(string topic, string filter)
    {
        // Handle exact match
        if (topic == filter)
            return true;

        var topicParts = topic.Split('/');
        var filterParts = filter.Split('/');

        // Multi-level wildcard
        if (filterParts.Length > 0 && filterParts[^1] == "#")
        {
            // Check all parts before #
            for (int i = 0; i < filterParts.Length - 1; i++)
            {
                if (i >= topicParts.Length)
                    return false;

                if (filterParts[i] != "+" && filterParts[i] != topicParts[i])
                    return false;
            }
            return true;
        }

        // Must have same number of levels
        if (topicParts.Length != filterParts.Length)
            return false;

        // Check each level
        for (int i = 0; i < filterParts.Length; i++)
        {
            if (filterParts[i] == "+")
                continue;

            if (filterParts[i] != topicParts[i])
                return false;
        }

        return true;
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
