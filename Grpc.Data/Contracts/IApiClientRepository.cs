using Grpc.Data.Entities;

namespace Grpc.Data.Contracts;

public interface IApiClientRepository
{
    Task<ApiClientDto> CreateClientAsync(ApiClientDto apiClientDto, CancellationToken token);
    Task<ApiClientDto?> GetClientByApiKeyAsync(string apiKey, CancellationToken token);
    Task<ApiClientSecretDto> CreateClientSecretAsync(ApiClientSecretDto apiClientSecretDto, CancellationToken token);
    Task<List<ApiClientSecretDto>> GetCurrentSecretAsync(string apiKey, CancellationToken token);
    Task<List<ApiGroupDto>> GetApiClientGroupsAsync(Guid apiClientId, CancellationToken token);
    Task<ApiGroupDto> CreateApiGroupAsync(string groupName, CancellationToken token);
    Task<ApiClientGroupDto> CreateApiClientGroupAsync(Guid apiClientId, int apiGroupId, CancellationToken token);
    Task<ApiGroupDto?> GetApiGroupByNameAsync(string groupName, CancellationToken token);
}
