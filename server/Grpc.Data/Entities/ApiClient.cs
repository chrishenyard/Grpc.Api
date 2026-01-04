using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grpc.Data.Entities;

[Table("ApiClient")]
public class ApiClient
{
    [Key]
    public Guid ApiClientId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public required string ApiKey { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public required string ClientName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ApiClientSecret> ApiClientSecrets { get; set; } = [];

    public ICollection<ApiClientGroup> ApiClientGroups { get; set; } = [];

    public RateLimit RateLimit { get; set; } = new();
}

[ComplexType]
public sealed class RateLimit
{
    public int PermitLimit { get; set; }
    public int QueueLimit { get; set; }
    public int WindowSeconds { get; set; }
}

public sealed record ApiClientDto
{
    public Guid ApiClientId { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
    public RateLimit RateLimit { get; init; } = new();

    public ApiClientDto() { }

    private ApiClientDto(Guid apiClientId, string apiKey, string clientName, bool isActive, DateTime createdUtc, RateLimit rateLimitConfig)
    {
        ValidateOrThrow(apiClientId, apiKey, clientName, createdUtc);

        ApiClientId = apiClientId;
        ApiKey = apiKey;
        ClientName = clientName;
        IsActive = isActive;
        CreatedUtc = createdUtc;
        RateLimit = rateLimitConfig;
    }

    public static ApiClientDto Create(Guid apiClientId, string apiKey, string clientName, RateLimit rateLimitConfig)
        => new(apiClientId, apiKey, clientName, true, DateTime.UtcNow, rateLimitConfig);

    public static implicit operator ApiClientDto(ApiClient entity)
        => new(entity.ApiClientId, entity.ApiKey, entity.ClientName, entity.IsActive, entity.CreatedUtc, entity.RateLimit)
        {
            RateLimit = entity.RateLimit
        };

    public static implicit operator ApiClient(ApiClientDto dto)
        => new()
        {
            ApiClientId = dto.ApiClientId,
            ApiKey = dto.ApiKey,
            ClientName = dto.ClientName,
            IsActive = dto.IsActive,
            CreatedUtc = dto.CreatedUtc,
            RateLimit = dto.RateLimit
        };

    private static void ValidateOrThrow(Guid apiClientId, string apiKey, string clientName, DateTime createdUtc)
    {
        if (apiClientId == Guid.Empty)
            throw new ArgumentException("ApiClientId must be provided.", nameof(apiClientId));

        ValidateOrThrow(apiKey, clientName, createdUtc);
    }

    private static void ValidateOrThrow(string apiKey, string clientName, DateTime createdUtc)
    {
        const int maxApiKeyLength = 100;
        if (apiKey.Length > maxApiKeyLength)
            throw new ArgumentException($"ApiKey cannot be longer than {maxApiKeyLength} characters.", nameof(apiKey));

        const int maxClientNameLength = 200;
        if (clientName.Length > maxClientNameLength)
            throw new ArgumentException($"ClientName cannot be longer than {maxClientNameLength} characters.", nameof(clientName));

        if (createdUtc == default)
            throw new ArgumentException("CreatedUtc must be set.", nameof(createdUtc));
    }
}

public static class ApiClientExtensions
{
    public static ApiClientDto ToApiClientDto(this ApiClient entity) => entity;
    public static ApiClient ToApiClient(this ApiClientDto dto) => dto;
}