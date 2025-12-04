using Grpc.Data.Contracts;
using Grpc.Data.DbContexts;
using Grpc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grpc.Data.Repositories;

public class ApiClientRepository(GrpcDbContext grpcDbContext) : IApiClientRepository
{
    private readonly GrpcDbContext _grpcDbContext = grpcDbContext;

    public async Task<ApiClientGroupDto> CreateApiClientGroupAsync(Guid apiClientId, int apiGroupId, CancellationToken token)
    {
        var apiClientGroup = new ApiClientGroup
        {
            ApiClientId = apiClientId,
            ApiGroupId = apiGroupId
        };
        await _grpcDbContext.ApiClientGroups.AddAsync(apiClientGroup, token);
        await _grpcDbContext.SaveChangesAsync(token);
        return apiClientGroup;
    }

    public async Task<ApiGroupDto> CreateApiGroupAsync(string groupName, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));

        var apiGroup = new ApiGroup
        {
            GroupName = groupName
        };

        await _grpcDbContext.ApiGroups.AddAsync(apiGroup, token);
        await _grpcDbContext.SaveChangesAsync(token);

        return apiGroup;
    }

    public async Task<ApiClientDto> CreateClientAsync(ApiClientDto apiClientDto, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(apiClientDto, nameof(apiClientDto));

        var apiClient = apiClientDto.ToApiClient();
        await _grpcDbContext.ApiClients.AddAsync(apiClient, token);
        await _grpcDbContext.SaveChangesAsync(token);

        return apiClient.ToApiClientDto();
    }

    public async Task<ApiClientSecretDto> CreateClientSecretAsync(ApiClientSecretDto apiClientSecretDto, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(apiClientSecretDto, nameof(apiClientSecretDto));

        var apiClientSecret = apiClientSecretDto.ToApiClientSecret();
        await _grpcDbContext.ApiClientSecrets.AddAsync(apiClientSecret, token);
        await _grpcDbContext.SaveChangesAsync(token);

        return apiClientSecret.ToApiClientSecretDto();
    }

    public Task<List<ApiGroupDto>> GetApiClientGroupsAsync(Guid apiClientId, CancellationToken token)
    {
        return _grpcDbContext.ApiClientGroups
            .AsNoTracking()
            .Include(cg => cg.ApiGroup)
            .Where(cg => cg.ApiClientId == apiClientId)
            .Select(cg => cg.ApiGroup.ToApiGroupDto())
            .ToListAsync(token);
    }

    public async Task<ApiGroupDto?> GetApiGroupByNameAsync(string groupName, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));

        var apiGroup = await _grpcDbContext.ApiGroups
            .FirstOrDefaultAsync(ag => ag.GroupName == groupName, token);

        return apiGroup?.ToApiGroupDto();
    }

    public async Task<ApiClientDto?> GetClientByApiKeyAsync(string apiKey, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));

        var apiClient = await _grpcDbContext.ApiClients
            .FirstOrDefaultAsync(ac => ac.ApiKey == apiKey && ac.IsActive == true, token);
        return apiClient?.ToApiClientDto();
    }

    public async Task<ApiClientSecretDto?> GetCurrentSecretAsync(string apiKey, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));

        var client = await _grpcDbContext.ApiClients
            .Where(c => c.ApiKey == apiKey && c.IsActive)
            .Select(c => new
            {
                Secrets = c.ApiClientSecrets
                    .Where(s => s.IsCurrent && (s.ExpiresUtc == null || s.ExpiresUtc > DateTime.UtcNow))
                    .OrderByDescending(s => s.CreatedUtc)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(token);

        var apiClientSecret = client?.Secrets;
        return apiClientSecret?.ToApiClientSecretDto();
    }
}
