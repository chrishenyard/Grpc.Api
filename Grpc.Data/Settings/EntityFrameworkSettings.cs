namespace Grpc.Data.Settings;

public class EntityFrameworkSettings
{
    public const string Section = "EntityFrameworkSettings";

    public int BatchSize { get; set; }
    public int CommandTimeout { get; set; }
    public int BulkRowCount { get; set; }
    public int MaxRetryCount { get; set; }
    public int MaxRetryDelay { get; set; }
}
