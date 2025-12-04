using Grpc.Data.Contracts;
using Grpc.Data.DbContexts;
using Grpc.Data.Repositories;
using Grpc.Data.Settings;
using Grpc.Service.Authorization;
using Grpc.Service.DataInitializer;
using Grpc.Service.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Grpc.Service.Configuration;

public static class ServiceExtensions
{
    public static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        builder.Configuration.AddConfiguration(configuration);

        return builder;
    }

    public static IServiceCollection AddDbContext(this IServiceCollection services)
    {
        services.AddDbContext<GrpcDbContext>((serviceProvider, options) =>
        {
            var entityFrameworkSettings = serviceProvider
                .GetRequiredService<IOptions<EntityFrameworkSettings>>()
                .Value;
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("DatabaseContext");

            options.LogTo(message => Debug.WriteLine(message))
            .UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.CommandTimeout(entityFrameworkSettings.CommandTimeout);
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: entityFrameworkSettings.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(entityFrameworkSettings.MaxRetryDelay),
                    errorNumbersToAdd: null);
            });
        });

        return services;
    }

    public static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<EntityFrameworkSettings>()
            .Bind(builder.Configuration.GetSection(EntityFrameworkSettings.Section))
            .ValidateDataAnnotations();

        return builder;
    }

    public static async Task EnsureDatabaseIfDevelopment(this WebApplication app, CancellationToken token)
    {
        if (app.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
            var initializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>();

            try
            {
                await initializer.InitializeData(token);
                logger.LogInformation("Database created (Development)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create database");
                throw;
            }
        }
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IApiClientRepository, ApiClientRepository>()
                .AddScoped<IJobRepository, JobRepository>()
                .AddScoped<IDataInitializer, ApiClientInitializer>();

        services.AddAuthentication(ApiClientAuthHandler.SchemeName)
                .AddScheme<ApiClientAuthOptions, ApiClientAuthHandler>(ApiClientAuthHandler.SchemeName, options => { })
                .Services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration config)
    {
        var seqSettings = config.GetSection("SeqSettings")
            .Get<SeqSettings>()!;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Grpc.Service"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(seqSettings.ServerUrl);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"X-Seq-ApiKey={seqSettings.ApiKey}";
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(seqSettings.ServerUrl);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"X-Seq-ApiKey={seqSettings.ApiKey}";
                }))
            .WithLogging(logging => logging
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(seqSettings.ServerUrl);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"X-Seq-ApiKey={seqSettings.ApiKey}";
                }));

        return services;
    }
}
