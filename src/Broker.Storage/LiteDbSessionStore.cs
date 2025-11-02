using Broker.Core.Qos;
using Broker.Core.Storage.Models;
using Broker.Storage.Models;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Broker.Storage;

// ============================================================================
// SESSION STORE IMPLEMENTATION
// ============================================================================

public class LiteDbSessionStore : ISessionStore, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILogger<LiteDbSessionStore> _logger;
    private readonly ILiteCollection<SessionDocument> _sessions;

    public LiteDbSessionStore(LiteDbProvider liteDbProvider, ILogger<LiteDbSessionStore> logger)
    {
        _logger = logger;
        _db = liteDbProvider.Database;

        _sessions = _db.GetCollection<SessionDocument>("sessions");
        _sessions.EnsureIndex(x => x.Id, unique: true);
        //_sessions.EnsureIndex(x => x.LastConnectedAt);
    }

    public async Task<MqttSession?> GetSessionAsync(string clientId, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            var doc = _sessions.FindById(clientId);
            if (doc == null)
            {
                _logger.LogDebug("Session not found for client {ClientId}", clientId);
                return null;
            }

            return MapToSession(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session for {ClientId}", clientId);
            throw;
        }
    }

    public async Task SaveSessionAsync(MqttSession session, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            var doc = new SessionDocument
            {
                Id = session.ClientId,
                CleanSession = session.CleanSession,
                CreatedAt = session.CreatedAt,
                LastConnectedAt = session.LastUpdated,
                WillQoS = session.WillMessage?.QoS,
                WillRetain = session.WillMessage?.Retain,
                WillTopic = session.WillMessage?.Topic,
                WillPayload = session.WillMessage?.Payload
            };

            _sessions.Upsert(doc);

            _logger.LogDebug("Session saved for client {ClientId}", session.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session for {ClientId}", session.ClientId);
            throw;
        }
    }

    public async Task DeleteSessionAsync(string clientId, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            _sessions.Delete(clientId);
            _logger.LogDebug("Session deleted for client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session for {ClientId}", clientId);
            throw;
        }
    }

    public async Task<bool> SessionExistsAsync(string clientId, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            return _sessions.Exists(x => x.Id == clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence for {ClientId}", clientId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAllClientIdsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            return _sessions.Query()
                .Select(x => x.Id)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all client IDs");
            throw;
        }
    }

    public async Task CleanupExpiredSessionsAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        try
        {
            var cutoff = DateTime.UtcNow - maxAge;
            var expired = _sessions.Find(x => x.LastConnectedAt < cutoff && x.CleanSession).ToList();

            foreach (var session in expired)
            {
                _sessions.Delete(session.Id);
            }

            if (expired.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired sessions", expired.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            throw;
        }
    }

    private static MqttSession MapToSession(SessionDocument doc)
    {
        var session = new MqttSession
        {
            ClientId = doc.Id,
            CleanSession = doc.CleanSession,
            CreatedAt = doc.CreatedAt,
            LastUpdated = doc.LastConnectedAt,
        };
        if (doc.WillTopic != null)
        {
            session.WillMessage = new WillMessage(doc.WillTopic ?? "", doc.WillPayload, doc.WillQoS ?? Core.QoSLevel.AtMostOnce, doc.WillRetain ?? false);
        }
        return session;
    }


    public void Dispose()
    {
        _db?.Dispose();
    }
}
