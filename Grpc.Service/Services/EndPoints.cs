using Grpc.Common.Utilities;
using Grpc.Data.Contracts;
using Grpc.Data.DbContexts;
using Grpc.Data.Entities;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Grpc.Service.Services;

public class EndPoints
{
    public static void Map(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapGet("/Authorization/{method}/{group}", async (
            string method,
            string group,
            IApiClientRepository apiClientRepository,
            CancellationToken token) =>
        {
            var (apiKey, secret) = GetCredentialsForGroup(group);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var rpcMethod = $"/grpc.jobs.v1.Jobs/{method}";
            var stringToSign = $"{rpcMethod}:{timestamp}";

            var signature = SecurityHelper.ComputeHmacSha512(secret, stringToSign);
            var apiClientSecretDto = await apiClientRepository.GetCurrentSecretAsync(apiKey, token);
            var secretHash = SecurityHelper.ComputeSecretHash(secret, apiClientSecretDto!.Salt);

            var headers = new Dictionary<string, string>
            {
                { "timestamp", timestamp },
                { "signature", signature }
            };

            return Results.Ok(new { Data = headers });
        });

        app.MapPut("/CreateTestJob", async (GrpcDbContext dbContext, CancellationToken token) =>
        {
            var initialJobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var initialJob = await dbContext.Jobs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.JobId == initialJobId, token);

            if (initialJob != null)
            {
                await dbContext.Jobs
                .IgnoreQueryFilters()
                .Where(j => j.JobId == initialJobId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.IsActive, true),
                cancellationToken: token);

                return Results.Ok();
            }

            initialJob = new Job
            {
                JobId = initialJobId,
                Name = "Initial Job",
                IsActive = true,
                Description = "This is the initial job created during data initialization.",
                CreatedUtc = DateTime.UtcNow
            };

            await dbContext.Jobs.AddAsync(initialJob, token);
            await dbContext.SaveChangesAsync(token);

            return Results.Ok();
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json; charset=utf-8";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { status = report.Status.ToString() }));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var payload = new
                {
                    status = report.Status.ToString(),
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs = e.Value.Duration.TotalMilliseconds,
                        exception = e.Value.Exception?.Message
                    })
                };

                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
        });
    }

    private static (string apiKey, string secret) GetCredentialsForGroup(string group)
    {
        return group.ToLowerInvariant() switch
        {
            "jobadmin" => ("test-client-001", "test-secret-001"),
            "jobreader" => ("test-client-002", "test-secret-002"),
            "jobwriter" => ("test-client-003", "test-secret-003"),
            _ => throw new ArgumentException("Invalid group specified.")
        };
    }
}
