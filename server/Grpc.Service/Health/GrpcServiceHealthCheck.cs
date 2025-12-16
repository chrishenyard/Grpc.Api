using Grpc.Data.DbContexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Grpc.Service.Health;

public class GrpcServiceHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GrpcDbContext>();

            var canConnect = await db.Database.CanConnectAsync(token);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Unable to connect to the database.");
            }

            return HealthCheckResult.Healthy("OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
