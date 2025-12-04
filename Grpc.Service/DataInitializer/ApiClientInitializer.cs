using Grpc.Common.Utilities;
using Grpc.Data.Contracts;
using Grpc.Data.Entities;

namespace Grpc.Service.DataInitializer;

public class ApiClientInitializer(
    IWebHostEnvironment webHostEnvironment,
    IApiClientRepository apiClientRepository) : IDataInitializer
{
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private readonly IApiClientRepository _apiClientRepository = apiClientRepository;

    public async Task InitializeData(CancellationToken token)
    {
        if (!_webHostEnvironment.IsDevelopment())
        {
            return;
        }

        // Job admin group
        var existingClient = await _apiClientRepository.GetClientByApiKeyAsync("test-client-001", token);

        if (existingClient == null)
        {
            var apiClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var apiClientSecretId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var apiSecret = "test-secret-001";
            var apiKey = "test-client-001";

            var salt = SecurityHelper.ComputeSalt();
            var apiSecretHash = SecurityHelper.ComputeSecretHash(apiSecret, salt);
            var apiClientDto = ApiClientDto.Create(apiClientId, apiKey, "Job Admin Group Test Client");
            var apiClientSecretDto = ApiClientSecretDto.Create(apiClientSecretId, apiClientId, salt, apiSecretHash);

            await _apiClientRepository.CreateClientAsync(apiClientDto, token);
            await _apiClientRepository.CreateClientSecretAsync(apiClientSecretDto, token);
            await InitializeApiGroup(apiClientId, "JobAdmin", token);
            await InitializeApiGroup(apiClientId, "JobReader", token);
            await InitializeApiGroup(apiClientId, "JobWriter", token);
        }

        // Job reader group
        existingClient = await _apiClientRepository.GetClientByApiKeyAsync("test-client-002", token);

        if (existingClient == null)
        {
            var apiClientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var apiClientSecretId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var apiSecret = "test-secret-002";
            var apiKey = "test-client-002";

            var salt = SecurityHelper.ComputeSalt();
            var apiSecretHash = SecurityHelper.ComputeSecretHash(apiSecret, salt);
            var apiClientDto = ApiClientDto.Create(apiClientId, apiKey, "Job Reader Group Test Client");
            var apiClientSecretDto = ApiClientSecretDto.Create(apiClientSecretId, apiClientId, salt, apiSecretHash);

            await _apiClientRepository.CreateClientAsync(apiClientDto, token);
            await _apiClientRepository.CreateClientSecretAsync(apiClientSecretDto, token);
            await InitializeApiGroup(apiClientId, "JobReader", token);
        }

        // Job writer group
        existingClient = await _apiClientRepository.GetClientByApiKeyAsync("test-client-003", token);

        if (existingClient == null)
        {
            var apiClientId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var apiClientSecretId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var apiSecret = "test-secret-003";
            var apiKey = "test-client-003";

            var salt = SecurityHelper.ComputeSalt();
            var apiSecretHash = SecurityHelper.ComputeSecretHash(apiSecret, salt);
            var apiClientDto = ApiClientDto.Create(apiClientId, apiKey, "Job Writer Group Test Client");
            var apiClientSecretDto = ApiClientSecretDto.Create(apiClientSecretId, apiClientId, salt, apiSecretHash);

            await _apiClientRepository.CreateClientAsync(apiClientDto, token);
            await _apiClientRepository.CreateClientSecretAsync(apiClientSecretDto, token);
            await InitializeApiGroup(apiClientId, "JobWriter", token);
        }
    }

    private async Task InitializeApiGroup(Guid apiClientId, string groupName, CancellationToken token)
    {
        var apiGroup = await _apiClientRepository.GetApiGroupByNameAsync(groupName, token);
        apiGroup ??= await _apiClientRepository.CreateApiGroupAsync(groupName, token);

        var apiClientGroups = await _apiClientRepository.GetApiClientGroupsAsync(apiClientId, token);

        if (apiClientGroups.Any(ag => ag.GroupName == groupName))
        {
            return;
        }

        _ = await _apiClientRepository.CreateApiClientGroupAsync(apiClientId, apiGroup.ApiGroupId, token);
    }
}
