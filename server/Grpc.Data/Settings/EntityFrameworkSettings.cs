using System.ComponentModel.DataAnnotations;

namespace Grpc.Data.Settings;

public class EntityFrameworkSettings
{
    public const string Section = "EntityFrameworkSettings";

    [Range(1000, 50000)]
    public int BatchSize { get; set; }

    [Range(5, 30)]
    public int CommandTimeout { get; set; }


    [Range(1000, 50000)]
    public int BulkRowCount { get; set; }

    [Range(1, 5)]
    public int MaxRetryCount { get; set; }

    [Range(1, 5)]
    public int MaxRetryDelay { get; set; }
}
