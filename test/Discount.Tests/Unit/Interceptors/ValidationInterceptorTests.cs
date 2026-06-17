using Discount.Grpc;
using Discount.Grpc.Interceptors;
using Discount.Grpc.Services;
using FluentValidation;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Discount.Tests.Support;

namespace Discount.Tests.Unit.Interceptors;

public class ValidationInterceptorTests
{
    [Fact]
    public async Task UnaryServerHandler_WithInvalidRequest_ThrowsInvalidArgument()
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssembly(typeof(DiscountService).Assembly);
        var interceptor = new ValidationInterceptor(services.BuildServiceProvider());
        var context = CreateContext();
        var wasContinued = false;

        var exception = await Assert.ThrowsAsync<RpcException>(() => interceptor.UnaryServerHandler(
            new GetDiscountRequest { ProductName = string.Empty },
            context,
            (_, _) =>
            {
                wasContinued = true;
                return Task.FromResult(new CouponModel());
            }));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.False(wasContinued);
    }

    [Fact]
    public async Task UnaryServerHandler_WithValidRequest_CallsContinuation()
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssembly(typeof(DiscountService).Assembly);
        var interceptor = new ValidationInterceptor(services.BuildServiceProvider());
        var context = CreateContext();
        var wasContinued = false;

        var response = await interceptor.UnaryServerHandler(
            new GetDiscountRequest { ProductName = "IPhone X" },
            context,
            (_, _) =>
            {
                wasContinued = true;
                return Task.FromResult(new CouponModel { ProductName = "IPhone X", Amount = 10 });
            });

        Assert.True(wasContinued);
        Assert.Equal("IPhone X", response.ProductName);
    }

    private static ServerCallContext CreateContext()
    {
        return new FakeServerCallContext();
    }
}
