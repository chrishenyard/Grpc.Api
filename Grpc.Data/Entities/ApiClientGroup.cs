using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grpc.Data.Entities;

[Table("ApiClientGroup")]
[PrimaryKey(nameof(ApiClientId), nameof(ApiGroupId))]
public class ApiClientGroup
{
    public Guid ApiClientId { get; set; }
    public int ApiGroupId { get; set; }
    public ApiClient ApiClient { get; set; } = null!;
    public ApiGroup ApiGroup { get; set; } = null!;
}

public class ApiClientGroupDto
{
    public Guid ApiClientId { get; init; }
    public int ApiGroupId { get; init; }
    private ApiClientGroupDto() { }
    private ApiClientGroupDto(Guid apiClientId, int apiGroupId)
    {
        ValidateOrThrow(apiClientId, apiGroupId);
        ApiClientId = apiClientId;
        ApiGroupId = apiGroupId;
    }
    public static ApiClientGroupDto Create(Guid apiClientId, int apiGroupId)
        => new(apiClientId, apiGroupId);
    public static implicit operator ApiClientGroupDto(ApiClientGroup apiClientGroup)
        => new(apiClientGroup.ApiClientId, apiClientGroup.ApiGroupId);
    public static implicit operator ApiClientGroup(ApiClientGroupDto apiClientGroupDto)
        => new()
        {
            ApiClientId = apiClientGroupDto.ApiClientId,
            ApiGroupId = apiClientGroupDto.ApiGroupId
        };
    private static void ValidateOrThrow(Guid apiClientId, int apiGroupId)
    {
        if (apiClientId == Guid.Empty)
            throw new ArgumentException("ApiClientId cannot be empty.", nameof(apiClientId));
        if (apiGroupId <= 0)
            throw new ArgumentOutOfRangeException(nameof(apiGroupId), "ApiGroupId must be greater than zero.");
    }
}

public static class ApiClientGroupExtensions
{
    public static ApiClientGroup ToApiClientGroup(this ApiClientGroupDto dto)
        => dto;
    public static ApiClientGroupDto ToApiClientGroupDto(this ApiClientGroup entity)
        => entity;
}

