using Grpc.Service.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace Grpc.Service.RateLimiters;

public class ApiClientIdRateLimiterPolicy(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<ApiClientIdRateLimiterPolicy> logger) : IRateLimiterPolicy<string>
{
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected = (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        return ValueTask.CompletedTask;
    };
    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;
    private readonly ILogger<ApiClientIdRateLimiterPolicy> _logger = logger;

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get => _onRejected; }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var apiClientId = httpContext.User.GetClaim(ApiClaimTypes.ApiClientId) ??
            throw new InvalidOperationException("No ApiClientId claim found in the token.");

        _ = int.TryParse(httpContext.User.GetClaim(ApiClaimTypes.ApiClientPermitLimit), out var apiClientPermitLimit);
        _ = int.TryParse(httpContext.User.GetClaim(ApiClaimTypes.ApiClientQueueLimit), out var apiClientQueueLimit);
        _ = int.TryParse(httpContext.User.GetClaim(ApiClaimTypes.ApiClientWindowSeconds), out var apiClientWindowSeconds);

        var rateLimit = new
        {
            PermitLimit = apiClientPermitLimit > 0 ? apiClientPermitLimit : 100,
            QueueLimit = apiClientQueueLimit > 0 ? apiClientQueueLimit : 0,
            WindowSeconds = apiClientWindowSeconds > 0 ? apiClientWindowSeconds : 60,
        };

        _logger.LogInformation("Client: {apiClientId} PermitLimit: {apiClientPermitLimit}", apiClientId, apiClientPermitLimit);

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(apiClientId, key => new RedisFixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
            PermitLimit = rateLimit.PermitLimit,
            ConnectionMultiplexerFactory = () => _connectionMultiplexer
        });
    }
}