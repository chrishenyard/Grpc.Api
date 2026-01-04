using Grpc.Data.Contracts;
using Grpc.Data.DbContexts;
using Grpc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Grpc.Data.Repositories;

public class ApiClientRepository(
    GrpcDbContext grpcDbContext,
    HybridCache cache) : IApiClientRepository
{
    private readonly GrpcDbContext _grpcDbContext = grpcDbContext;
    private readonly HybridCache _cache = cache;

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

    public async Task<List<ApiGroupDto>> GetApiClientGroupsAsync(Guid apiClientId, CancellationToken token)
    {
        var groups = await _cache.GetOrCreateAsync<List<ApiGroupDto>>(
            $"ApiClientGroups_{apiClientId}",
            async ct =>
            {
                var result = await _grpcDbContext.ApiClientGroups
                    .AsNoTracking()
                    .Include(cg => cg.ApiGroup)
                    .Where(cg => cg.ApiClientId == apiClientId)
                    .Select(cg => cg.ApiGroup.ToApiGroupDto())
                    .ToListAsync(ct);

                return result;
            },
            tags: ["api", "client", "groups"],
            cancellationToken: token);

        return groups;
    }

    public async Task<ApiGroupDto?> GetApiGroupByNameAsync(string groupName, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));

        var apiGroup = await _cache.GetOrCreateAsync<ApiGroupDto?>(
            $"ApiGroup_{groupName}",
            async ct =>
            {
                var result = await _grpcDbContext.ApiGroups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ag => ag.GroupName == groupName, ct);

                return result?.ToApiGroupDto();
            },
            tags: ["apigroup"],
            cancellationToken: token);

        return apiGroup;
    }

    public async Task<ApiClientDto?> GetClientByApiKeyAsync(string apiKey, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));

        var apiClient = await _cache.GetOrCreateAsync<ApiClientDto?>(
            $"ApiClient_{apiKey}",
            async ct =>
            {
                var result = await _grpcDbContext.ApiClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ac => ac.ApiKey == apiKey && ac.IsActive == true, ct);

                return result?.ToApiClientDto();
            },
            tags: ["api", "client"],
            cancellationToken: token);

        return apiClient;
    }

    public async Task<List<ApiClientSecretDto>> GetCurrentSecretAsync(string apiKey, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));

        var apiClientSecrets = await _cache.GetOrCreateAsync<List<ApiClientSecretDto>>(
            $"ApiClientSecrets_{apiKey}",
            async ct =>
            {
                var secrets = await _grpcDbContext.ApiClientSecrets
                    .Include(s => s.ApiClient)
                    .AsNoTracking()
                    .Where(s => s.ApiClient.ApiKey == apiKey
                                && s.ApiClient.IsActive
                                && (s.ExpiresUtc == null || s.ExpiresUtc > DateTime.UtcNow))
                    .ToListAsync(ct);

                var dtos = secrets
                    .Select(s => s.ToApiClientSecretDto())
                    .ToList();

                return dtos;
            },
            tags: ["api", "client", "secrets"],
            cancellationToken: token);

        return apiClientSecrets;
    }
}
