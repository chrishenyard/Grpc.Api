using Grpc.Common.Utilities;
using Grpc.Data.Contracts;
using Grpc.Service.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Grpc.Service.Authorization;

public class ApiClientAuthHandler(
    IOptionsMonitor<ApiClientAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiClientRepository apiClientRepository,
    IHttpContextAccessor httpContextAccessor) : AuthenticationHandler<ApiClientAuthOptions>(options, logger, encoder)
{
    private readonly ILogger _logger = logger.CreateLogger<ApiClientAuthHandler>();
    private readonly IApiClientRepository _repository = apiClientRepository;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private static readonly TimeSpan AllowedSkew = TimeSpan.FromMinutes(5);

    public const string SchemeName = "ApiClientScheme";
    private const string InvalidAuthenticationMessage = "Invalid authentication";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint == null)
        {
            return AuthenticateResult.NoResult();
        }

        var requiresAuth = endpoint.Metadata.GetMetadata<RequireApiGroupAttribute>() != null;
        if (!requiresAuth)
        {
            return AuthenticateResult.NoResult();
        }

        (bool flowControl, AuthenticateResult? value) = TryGetApiCredentialsFromHeaders(
            out string apiKey, out string apiSecret, out string timestamp, out string signature);

        if (!flowControl)
        {
            return value!;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null in ApiClientAuthHandler.");
            return AuthenticateResult.Fail("Unable to complete authentication");
        }

        if (!long.TryParse(timestamp, out var tsSeconds))
        {
            _logger.LogInformation("Timestamp expired: {Timestamp}", timestamp);
            return AuthenticateResult.Fail(InvalidAuthenticationMessage);
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(tsSeconds);
        var now = DateTimeOffset.UtcNow;

        if (now - requestTime > AllowedSkew || requestTime - now > AllowedSkew)
        {
            _logger.LogInformation("Expired request. Request time: {RequestTime}, Now: {Now}", requestTime, now);
            return AuthenticateResult.Fail(InvalidAuthenticationMessage);
        }

        var apiClientSecretDto = await _repository.GetCurrentSecretAsync(apiKey, httpContext.RequestAborted);

        if (apiClientSecretDto == null)
        {
            _logger.LogInformation("API key not found: {ApiKey}", apiKey);
            return AuthenticateResult.Fail(InvalidAuthenticationMessage);
        }

        var apiClientDto = await _repository.GetClientByApiKeyAsync(apiKey, httpContext.RequestAborted);

        if (apiClientDto == null || !apiClientDto.IsActive)
        {
            _logger.LogInformation("API client not found or inactive for API key: {ApiKey}", apiKey);
            return AuthenticateResult.Fail(InvalidAuthenticationMessage);
        }

        var rpcMethod = GetRpcMethodFromRequest(Context);
        var stringToSign = $"{rpcMethod}:{timestamp}";
        var expectedSignature = SecurityHelper.ComputeHmacSha512(apiSecret, stringToSign);

        if (!string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Invalid signature. Expected: {ExpectedSignature}, Provided: {ProvidedSignature}", expectedSignature, signature);
            return AuthenticateResult.Fail(InvalidAuthenticationMessage);
        }

        var apiClientGroups = await _repository.GetApiClientGroupsAsync(apiClientDto.ApiClientId, httpContext.RequestAborted);
        var groupNames = apiClientGroups.Select(g => g.GroupName).ToList();

        var claims = new List<Claim>
        {
            new("api-client-id", apiClientDto.ApiClientId.ToString()),
            new("api-client-groups", string.Join(",", groupNames)),
            new (ClaimTypes.Name, apiKey)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private (bool flowControl, AuthenticateResult? value) TryGetApiCredentialsFromHeaders(out string apiKey, out string apiSecret, out string timestamp, out string signature)
    {
        apiKey = string.Empty;
        apiSecret = string.Empty;
        timestamp = string.Empty;
        signature = string.Empty;

        var request = Context.Request;
        if (!request.Headers.TryGetValue("x-api-key", out var apiKeyValues) ||
            !request.Headers.TryGetValue("x-api-secret", out var apiScretValues) ||
            !request.Headers.TryGetValue("x-timestamp", out var timestampValues) ||
            !request.Headers.TryGetValue("x-signature", out var signatureValues))
        {
            return (flowControl: false, value: AuthenticateResult.NoResult());
        }

        apiKey = apiKeyValues.ToString();
        apiSecret = apiScretValues.ToString();
        timestamp = timestampValues.ToString();
        signature = signatureValues.ToString();
        return (flowControl: true, value: null);
    }

    private static string GetRpcMethodFromRequest(HttpContext httpContext)
    {
        // In ASP.NET Core gRPC, the HTTP :path is the fully-qualified gRPC method
        // e.g. "/grpc.jobs.v1.Jobs/GetJobs"
        return httpContext.Request.Path.ToString();
    }
}
