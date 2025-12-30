using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Data.Contracts;
using Grpc.Service.Attributes;
using Grpc.Service.Extensions;
using System.Reflection;

namespace Grpc.Service.Interceptors;


public class ApiGroupAuthInterceptor(
    IApiClientRepository repository,
    ILogger<ApiGroupAuthInterceptor> logger) : Interceptor
{
    private readonly IApiClientRepository _repository = repository;
    private readonly ILogger<ApiGroupAuthInterceptor> _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        var (methodAttrs, classAttrs) = GetRequireApiGroupAttributes(context);
        // Prefer method-level attributes. If none, use class-level.
        var requiredGroups = methodAttrs.Count != 0 ? methodAttrs : classAttrs;

        if (requiredGroups.Count == 0)
        {
            return await base.UnaryServerHandler(request, context, continuation);
        }

        var apiKey = httpContext.User.GetClaim(ApiClaimTypes.ApiKey);

        if (apiKey == null)
        {
            _logger.LogInformation("Can't resolve API Key");
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "Unauthorized"));
        }

        var apiClientGroups = httpContext.User.GetClaims(ApiClaimTypes.ApiClientGroup);

        foreach (var attr in requiredGroups)
        {
            var inGroup = apiClientGroups.Contains(attr.GroupName, StringComparer.OrdinalIgnoreCase);

            if (!inGroup)
            {
                _logger.LogInformation("Not authorized for group {Group}.", attr.GroupName);
                throw new RpcException(new Status(
                    StatusCode.PermissionDenied,
                    $"Unauthorized"));
            }
        }

        return await base.UnaryServerHandler(request, context, continuation);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        var (methodAttrs, classAttrs) = GetRequireApiGroupAttributes(context);
        // Prefer method-level attributes. If none, use class-level.
        var requiredGroups = methodAttrs.Count != 0 ? methodAttrs : classAttrs;

        if (requiredGroups.Count == 0)
        {
            await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            return;
        }

        var apiKey = httpContext.User.GetClaim(ApiClaimTypes.ApiKey);

        if (apiKey == null)
        {
            _logger.LogInformation("Can't resolve API Key");
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "Unauthorized"));
        }

        var apiClientGroups = httpContext.User.GetClaims(ApiClaimTypes.ApiClientGroup);

        foreach (var attr in requiredGroups)
        {
            var inGroup = apiClientGroups.Contains(attr.GroupName, StringComparer.OrdinalIgnoreCase);

            if (!inGroup)
            {
                _logger.LogInformation("Not authorized for group {Group}.", attr.GroupName);
                throw new RpcException(new Status(
                    StatusCode.PermissionDenied,
                    "Unauthorized"));
            }
        }

        await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
    }

    private static (IReadOnlyCollection<RequireApiGroupAttribute> methodAttrs, IReadOnlyCollection<RequireApiGroupAttribute> classAttrs)
    GetRequireApiGroupAttributes(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var endpoint = httpContext?.GetEndpoint();
        if (endpoint == null)
            return (Array.Empty<RequireApiGroupAttribute>(), Array.Empty<RequireApiGroupAttribute>());

        // 1) Prefer a MethodInfo on the endpoint
        var methodInfo = endpoint.Metadata.GetMetadata<MethodInfo>();
        if (methodInfo != null)
        {
            var methodAttrs = methodInfo.GetCustomAttributes<RequireApiGroupAttribute>(inherit: true).ToArray();
            var classAttrs = methodInfo.DeclaringType?.GetCustomAttributes<RequireApiGroupAttribute>(inherit: true).ToArray()
                             ?? [];
            return (methodAttrs, classAttrs);
        }

        // 2) Try GrpcMethodMetadata (present in most gRPC endpoint metadata)
        var grpcMethodMetadata = endpoint.Metadata.GetMetadata<AspNetCore.Server.GrpcMethodMetadata>();
        if (grpcMethodMetadata != null)
        {
            // grpcMethodMetadata.Method may not be a MethodInfo in every version; try both
            var possibleMethodInfo = grpcMethodMetadata.Method as MethodInfo
                                     ?? grpcMethodMetadata.ServiceType?.GetMethod(
                                         grpcMethodMetadata.Method.Name,
                                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (possibleMethodInfo != null)
            {
                var methodAttrs = possibleMethodInfo.GetCustomAttributes<RequireApiGroupAttribute>(inherit: true).ToArray();
                var classAttrs = possibleMethodInfo.DeclaringType?.GetCustomAttributes<RequireApiGroupAttribute>(inherit: true).ToArray()
                                 ?? [];
                return (methodAttrs, classAttrs);
            }
        }

        // 3) Last resort: attributes attached directly to the endpoint metadata.
        //    Can't reliably separate method vs class here, so return them as method-level by convention.
        var endpointAttrs = endpoint.Metadata.GetOrderedMetadata<RequireApiGroupAttribute>().ToArray();
        return (endpointAttrs, Array.Empty<RequireApiGroupAttribute>());
    }
}
