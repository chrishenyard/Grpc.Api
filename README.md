# gRPC Service Solution

Overview
--------
This solution is a gRPC-based microservice built on .NET 10 (C# 14). It exposes a `Jobs` gRPC API (defined by `Protos/jobs.proto`) and uses a modern server-side stack: dependency injection, EF Core for persistence, FluentValidation for request validation, OpenTelemetry for observability, and structured exception handling.

Tech stack
----------
- .NET 10, C# 14
- gRPC (via `Grpc.AspNetCore`) and Protobuf (file: `Grpc.Service/Protos/jobs.proto`)
- Entity Framework Core (SQL Server) for data access (`Grpc.Data` project)
- FluentValidation with DI integration (`FluentValidation.DependencyInjectionExtensions`)
- Server-side gRPC interceptors for cross-cutting concerns (authentication, validation)
  - `HmacAuthInterceptor` (auth)
  - `ValidationInterceptor` (reflection-based validator resolution)
- OpenTelemetry for metrics, tracing, and logs with OTLP exporter
- Global error handling via `GlobalExceptionHandler` and ProblemDetails
- Project layout follows standard solution modularization:
  - `Grpc.Service` — host / gRPC service
  - `Grpc.Data` — EF Core entities, repositories, DB context
  - `Grpc.Common` — shared types
  - `Grpc.Database` — SQL Server initialization/migrations

Key components
--------------
- Protobuf:
  - `Grpc.Service/Protos/jobs.proto` — service contract and message types.
  - MSBuild Proto integration is configured in `Grpc.Service.csproj` (server code generated at build).

- gRPC service:
  - `Grpc.Service.Services.JobService` — Implements the generated `Jobs.JobsBase` service.

- Validation:
  - Validators are discovered and registered with `AddValidatorsFromAssembly(...)` in `Program.cs`.
  - `ValidationInterceptor` resolves `IValidator<TRequest>` from DI using the runtime request type (reflection/dynamic) and runs validation for unary calls. Validation failures become `RpcException` with `StatusCode.InvalidArgument` and metadata detailing errors.

- Persistence:
  - `Grpc.Data` contains EF Core `GrpcDbContext`, entities (e.g., `Job`), and repositories (`IJobRepository`, `JobRepository`).
  - DB configuration and resilient SQL Server options are configured in `ServiceExtensions.AddDbContext`.

- Observability:
  - OpenTelemetry added in `Program.cs`:
    - Metrics + Tracing + Logging with console and OTLP exporters.
    - Resource configured with service name `Grpc.Service`.

- Error handling:
  - `GlobalExceptionHandler` implements `IExceptionHandler` to produce RFC-compliant `ProblemDetails` responses for unexpected server exceptions.

How to build & run
------------------
From repository root:

- Build
  - Include an .env file or user secrets for sensitive config (e.g., DB connection string, HMAC keys).
  - Run docker compose up to create SQL Server and other services.
  - Publish the database project to ensure the latest migrations are applied.
  - Run the gRPC service project in Docker.

How to test
---------------
- Use a gRPC client (e.g., `grpcurl`, Postman, or a custom .NET client) to call the `Jobs` service methods.
- Example `grpcurl` command to list jobs:
  ```
  grpcurl -plaintext -d '{}' localhost:5000 jobs.Jobs/ListJobs
  ```
- Run unit/integration tests via the `Grpc.Service.Client` project in the solution root.
- Check logs for OpenTelemetry traces and metrics output.
- Use health check endpoints (`/health/live` and `/health/ready`) to verify service liveness and readiness.
- Use `grpc_health_probe` for gRPC health checks if running in Docker/Kubernetes.

Notes for developers
--------------------
- Protobuf codegen: MSBuild generates server stubs automatically via the `Protobuf` item in `Grpc.Service.csproj`.
- Add validators by creating classes implementing `AbstractValidator<T>` (from FluentValidation). They will be discovered automatically when placed in the same assembly and registered by `AddValidatorsFromAssembly`.
  - Example: `public class JobAddRequestValidator : AbstractValidator<JobAddRequest> { ... }`
- The `ValidationInterceptor` centralizes request validation so individual service methods can remain focused on business logic.
- To change validation behavior (structured errors, google.rpc.Status details), extend the interceptor to return richer error payloads.
- Database initialization for development occurs via `EnsureDatabaseIfDevelopment` called during host startup.

Relevant files
--------------
- `Grpc.Service/Program.cs` — application startup, DI, interceptors, OpenTelemetry configuration.
- `Grpc.Service/Interceptors/ValidationInterceptor.cs` — reflection-based validator lookup and invocation.
- `Grpc.Service/Services/JobService.cs` — gRPC service implementation.
- `Grpc.Data/Entities/Job.cs` and DTOs — persistence and domain mapping.
- `Grpc.Service/Protos/jobs.proto` — service contract.

## Health checks (gRPC + HTTP)

This solution includes both HTTP health endpoints and a gRPC Health service so standard tooling can probe service liveness and readiness.

What was added
- A gRPC Health proto and server implementation (`grpc.health.v1`) that implements `Check` and a minimal `Watch` — exposed via `app.MapGrpcService<GrpcHealthService>()`.
- Integration with ASP.NET Core HealthChecks:
  - `GrpcServiceHealthCheck` (implements `IHealthCheck`) verifies critical dependencies (currently DB connectivity via `GrpcDbContext`) and is registered with the `"ready"` tag.
  - `/health/live` — liveness endpoint (fast, no dependency checks).
  - `/health/ready` — readiness endpoint (runs checks tagged `"ready"` and returns JSON details).
- Docker support for gRPC probes:
  - Add the `grpc_health_probe` binary to the image (recommended) and use it in the container `HEALTHCHECK` to probe the gRPC `Health.Check` method.
  - Fall back to HTTP readiness (`/health/ready`) if `grpc_health_probe` is not available.

How it works
- gRPC health probe:
  - `grpc_health_probe` calls the gRPC Health service `Check` method; the service uses the HealthCheckService to run readiness checks and returns SERVING / NOT_SERVING.
- HTTP endpoints:
  - Useful for Docker Compose and Kubernetes liveness/readiness probes and for manual debugging (curl).
- Health check tagging:
  - Only checks registered with the `"ready"` tag are executed for readiness; other checks can be added without affecting readiness.

Files to review
- `Protos/health.proto` — gRPC Health proto.
- `Grpc.Service/Services/GrpcHealthService.cs` — gRPC Health service implementation.
- `Grpc.Service/Health/GrpcServiceHealthCheck.cs` — application readiness check (DB connectivity).
- `Grpc.Service/Program.cs` — health checks registration and endpoint mappings.
- `Grpc.Service/Dockerfile` — add `grpc_health_probe` binary (optional) for accurate gRPC probes.
- `docker-compose.yml` — example healthcheck using `grpc_health_probe` or HTTP readiness.

Quick test examples
- HTTP readiness (container exposes port 8080):
  - curl -f http://127.0.0.1:8080/health/ready
- gRPC probe via included binary (inside container):
  - grpc_health_probe -addr=127.0.0.1:8080
- Docker Compose healthcheck example:
  - Use `grpc_health_probe` for gRPC or `curl` against `/health/ready` if `grpc_health_probe` is not provided.

Notes
- Ensure the health check logic is fast and resilient. Keep heavy checks out of liveness and limit readiness checks to startup-critical dependencies.
- Do not commit `grpc_health_probe` binary into source control; add it during Docker image build or fetch it from a trusted release during CI pipeline.

Contributing
------------
Follow the established project coding style, keep public APIs backwards compatible, and add unit/integration tests for behavior changes (validation, persistence, interceptors). Run `dotnet build` and ensure OpenTelemetry/local logging behaves as expected.
