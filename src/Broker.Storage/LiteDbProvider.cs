using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Broker.Storage;

public class LiteDbProvider
{
    public LiteDatabase Database { get; private set; }
    private readonly ILogger<LiteDbProvider> _logger;
    public LiteDbProvider(IOptions<StorageOptions> options, ILogger<LiteDbProvider> logger)
    {
        _logger = logger;
        var optionsValue = options.Value;

        // Ensure data directory exists
        var dataPath = Path.GetDirectoryName(optionsValue.DatabasePath);
        if (!string.IsNullOrEmpty(dataPath) && !Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        // Initialize LiteDB
        var connectionString = new ConnectionString
        {
            Filename = optionsValue.DatabasePath,
            Connection = ConnectionType.Shared
        };

        Database = new LiteDatabase(connectionString);
        _logger.LogInformation("LiteDB storage initialized at {DatabasePath}", optionsValue.DatabasePath);
    }
}
