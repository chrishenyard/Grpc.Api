using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Grpc.Service.Interceptors;

public class ValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateRequestAsync(request, context.CancellationToken);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        await ValidateRequestAsync(request, context.CancellationToken);
        await continuation(request, responseStream, context);
    }

    private async Task ValidateRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is Google.Protobuf.WellKnownTypes.Empty)
        {
            return;
        }

        var validator = _serviceProvider.GetService<IValidator<TRequest>>() ??
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"No validator for type '{typeof(TRequest).FullName}' has been registered."));

        var result = await validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid)
        {
            return;
        }

        var metadata = new Metadata();
        foreach (var error in result.Errors)
        {
            metadata.Add(error.PropertyName.ToLower(), error.ErrorMessage);
        }

        throw new RpcException(new Status(StatusCode.InvalidArgument, "validation errors"), metadata);
    }
}
