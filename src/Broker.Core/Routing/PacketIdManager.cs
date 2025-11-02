using System.Collections.Concurrent;

namespace Broker.Core.Routing;

/// <summary>
/// Manages packet ID assignment for QoS 1 and QoS 2 messages.
/// </summary>
public class PacketIdManager
{
    private readonly ConcurrentDictionary<string, ushort> _clientPacketIds = new();
    private readonly object _lock = new object();

    /// <summary>
    /// Gets the next available packet ID for a client.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <returns>The next packet ID (1-65535, wraps around).</returns>
    public ushort GetNextPacketId(string clientId)
    {
        lock (_lock)
        {
            // Get or create the last packet ID for this client
            ushort lastPacketId = _clientPacketIds.GetOrAdd(clientId, _ => 0);

            // Increment packet ID (wraps around at 65535)
            ushort nextId = (ushort)(lastPacketId + 1);
            if (nextId == 0)
            {
                nextId = 1; // Skip 0 (invalid packet ID)
            }

            _clientPacketIds[clientId] = nextId;
            return nextId;
        }
    }

    /// <summary>
    /// Releases a packet ID (marks it as available for reuse).
    /// This is called when a message is acknowledged.
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="packetId">Packet ID to release.</param>
    public void ReleasePacketId(string clientId, ushort packetId)
    {
        // For simplicity, we don't track released IDs individually
        // Packet IDs will naturally wrap around and reuse
        // In a production system, you might want to track in-use IDs per client
    }

    /// <summary>
    /// Clears all packet IDs for a client (e.g., on disconnect).
    /// </summary>
    /// <param name="clientId">Client identifier.</param>
    public void ClearClient(string clientId)
    {
        _clientPacketIds.TryRemove(clientId, out _);
    }
}

