using System.ComponentModel.DataAnnotations;

namespace Grpc.Service.Settings;

public class HybridCacheSettings
{
    public const string Section = "HybridCacheSettings";

    [Range(1, 10_485_760)]
    public int MaximumPayloadBytes { get; set; } = 1024 * 1024 * 10; // 10MB default

    [Range(1, 250)]
    public int MaximumKeyLength { get; set; } = 250;

    [Range(1, 1440)]
    public int LocalCacheExpirationFromMinutes { get; set; } = 5;

    [Range(1, 1440)]
    public int ExpirationFromMinutes { get; set; } = 5;
}
