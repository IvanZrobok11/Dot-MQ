namespace Broker.Storage;

/// <summary>
/// Configuration options for embedded storage.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Base directory path for storage data files.
    /// </summary>
    public required string DataPath { get; set; }

    /// <summary>
    /// Full path to the LiteDB database file.
    /// </summary>
    public required string DatabasePath { get; set; }

    /// <summary>
    /// Time period after which old sessions are cleaned up (default: 7 days).
    /// </summary>
    public TimeSpan SessionRetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Time period after which old pending messages are cleaned up (default: 1 day).
    /// </summary>
    public TimeSpan PendingMessageRetentionPeriod { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Time period after which old QoS 2 states are cleaned up (default: 1 day).
    /// </summary>
    public TimeSpan Qos2StateRetentionPeriod { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Interval at which cleanup runs (default: 1 hour).
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}

