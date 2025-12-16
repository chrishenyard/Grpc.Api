using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grpc.Data.Entities;

[Table("ApiGroup")]
public class ApiGroup
{
    [Key]
    public int ApiGroupId { get; set; }

    [Column(TypeName = "varchar(50)")]
    public required string GroupName { get; set; }

    public ICollection<ApiClientGroup> ApiClientGroups { get; set; } = new List<ApiClientGroup>();
}

public class ApiGroupDto
{
    public int ApiGroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    private ApiGroupDto() { }

    private ApiGroupDto(int apiGroupId, string groupName)
    {
        ValidateOrThrow(apiGroupId, groupName);
        ApiGroupId = apiGroupId;
        GroupName = groupName;
    }

    public static ApiGroupDto Create(int apiGroupId, string groupName)
        => new(apiGroupId, groupName);

    public static implicit operator ApiGroupDto(ApiGroup apiGroup)
        => new(apiGroup.ApiGroupId, apiGroup.GroupName);

    public static implicit operator ApiGroup(ApiGroupDto apiGroupDto)
        => new()
        {
            ApiGroupId = apiGroupDto.ApiGroupId,
            GroupName = apiGroupDto.GroupName
        };

    private static void ValidateOrThrow(int apiGroupId, string groupName)
    {
        if (apiGroupId <= 0)
            throw new ArgumentOutOfRangeException(nameof(apiGroupId), "ApiGroupId must be greater than zero.");
        ValidateOrThrow(groupName);
    }

    private static void ValidateOrThrow(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentException("GroupName cannot be null or empty.", nameof(groupName));
        if (groupName.Length > 50)
            throw new ArgumentException("GroupName cannot exceed 50 characters.", nameof(groupName));
    }
}

public static class ApiGroupExtensions
{
    public static ApiGroupDto ToApiGroupDto(this ApiGroup apiGroup) => apiGroup;
    public static ApiGroup ToApiGroup(this ApiGroupDto apiGroupDto) => apiGroupDto;
}
