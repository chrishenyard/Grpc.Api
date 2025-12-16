using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grpc.Data.Entities;

[Table("ApiClientSecret")]
public class ApiClientSecret
{
    [Key]
    public Guid ApiClientSecretId { get; set; }

    public Guid ApiClientId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public required string Salt { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public required string Secret { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresUtc { get; set; }

    public virtual ApiClient ApiClient { get; set; } = null!;
}

public sealed record ApiClientSecretDto
{
    public long ApiClientSecretInternalId { get; init; }
    public Guid ApiClientSecretId { get; init; }
    public Guid ApiClientId { get; init; }
    public string Salt { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public DateTime? ExpiresUtc { get; init; }

    private ApiClientSecretDto() { }

    private ApiClientSecretDto(Guid apiClientSecretId, Guid apiClientId, string salt, string secret, DateTime createdUtc, DateTime? expiresUtc)
    {
        ValidateOrThrow(apiClientSecretId, apiClientId, salt, secret, createdUtc, expiresUtc);

        ApiClientSecretId = apiClientSecretId;
        ApiClientId = apiClientId;
        Salt = salt;
        Secret = secret;
        CreatedUtc = createdUtc;
        ExpiresUtc = expiresUtc;
    }

    private ApiClientSecretDto(Guid apiClientId, string salt, string secret, DateTime createdUtc, DateTime? expiresUtc)
    {
        ValidateOrThrow(apiClientId, salt, secret, createdUtc, expiresUtc);

        ApiClientId = apiClientId;
        Salt = salt;
        Secret = secret;
        CreatedUtc = createdUtc;
        ExpiresUtc = expiresUtc;
    }

    public static ApiClientSecretDto Create(Guid apiClientId, string salt, string secret)
        => new(
            apiClientId,
            salt,
            secret,
            DateTime.UtcNow,
            null);

    public static ApiClientSecretDto Create(Guid apiClientSecretId, Guid apiClientId, string salt, string secret)
        => new(
            apiClientSecretId,
            apiClientId,
            salt,
            secret,
            DateTime.UtcNow,
            null);

    public static implicit operator ApiClientSecretDto(ApiClientSecret entity)
        => new(entity.ApiClientSecretId, entity.ApiClientId, entity.Salt, entity.Secret, entity.CreatedUtc, entity.ExpiresUtc);

    public static implicit operator ApiClientSecret(ApiClientSecretDto dto)
        => new()
        {
            ApiClientSecretId = dto.ApiClientSecretId,
            ApiClientId = dto.ApiClientId,
            Salt = dto.Salt,
            Secret = dto.Secret,
            CreatedUtc = dto.CreatedUtc,
            ExpiresUtc = dto.ExpiresUtc
        };

    private static void ValidateOrThrow(Guid apiClientSecretId, Guid apiClientId, string salt, string secret, DateTime createdUtc, DateTime? expiresUtc)
    {
        if (apiClientSecretId == Guid.Empty)
            throw new ArgumentException("ApiClientSecretId must be provided.", nameof(apiClientSecretId));

        ValidateOrThrow(apiClientId, salt, secret, createdUtc, expiresUtc);
    }

    private static void ValidateOrThrow(Guid apiClientId, string salt, string secret, DateTime createdUtc, DateTime? expiresUtc)
    {
        if (apiClientId == Guid.Empty)
            throw new ArgumentException("ApiClientId must be provided.", nameof(apiClientId));

        const int maxSecretLength = 200;
        if (secret.Length > maxSecretLength)
            throw new ArgumentException($"Secret cannot be longer than {maxSecretLength} characters.", nameof(secret));

        const int maxSaltLength = 100;
        if (salt.Length > maxSaltLength)
            throw new ArgumentException($"Salt cannot be longer than {maxSaltLength} characters.", nameof(salt));

        if (createdUtc == default)
            throw new ArgumentException("CreatedUtc must be set.", nameof(createdUtc));

        if (expiresUtc.HasValue && expiresUtc.Value <= createdUtc)
            throw new ArgumentException("ExpiresUtc, if set, must be later than CreatedUtc.", nameof(expiresUtc));
    }

}

public static class ApiClientSecretExtensions
{
    public static ApiClientSecret ToApiClientSecret(this ApiClientSecretDto dto)
        => dto;
    public static ApiClientSecretDto ToApiClientSecretDto(this ApiClientSecret entity)
        => entity;
}