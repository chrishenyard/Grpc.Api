using FluentValidation;
using Grpc.Service;
using Grpc.Service.Configuration;
using Grpc.Service.Health;
using Grpc.Service.Interceptors;
using Grpc.Service.Services;
using Grpc.Service.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration);
});

builder
    .AddConfiguration()
    .AddSettings();

var configuration = builder.Configuration;
var seqSettings = configuration.GetSection("SeqSettings")
    .Get<SeqSettings>()!;

builder.Services
    .AddTelemetry(configuration)
    .AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true)
    .AddProblemDetails()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddDbContext()
    .AddServices()
    .AddHttpContextAccessor()
    .AddGrpc(options =>
    {
        options.Interceptors.Add<GrpcStreamingExceptionInterceptor>();
    })
    .AddServiceOptions<JobService>(options =>
    {
        options.Interceptors.Add<ApiGroupAuthInterceptor>();
        options.Interceptors.Add<ValidationInterceptor>();
    })
    .Services.AddGrpcReflection();

builder.Services.AddGrpcHealthChecks()
                .AddCheck<GrpcServiceHealthCheck>("Database");

var app = builder.Build();

app.UseHttpsRedirection()
    .UseSerilogRequestLogging()
    .UseAuthentication()
    .UseAuthorization()
    .UseExceptionHandler();

await app.EnsureDatabaseIfDevelopment(app.Lifetime.ApplicationStopping);

app.MapGrpcService<JobService>();
app.MapGrpcHealthChecksService();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

EndPoints.Map(app);

app.Run();
