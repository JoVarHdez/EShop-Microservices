using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Discount.Grpc.Interceptors;

public class ValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>();

        if (validator is not null)
        {
            var result = await validator.ValidateAsync(request, context.CancellationToken);

            if (!result.IsValid)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }
        }

        return await continuation(request, context);
    }
}