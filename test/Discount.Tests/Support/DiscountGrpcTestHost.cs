using Discount.Grpc.Interceptors;
using Discount.Grpc.Repository;
using Discount.Grpc.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Discount.Tests.Support;

public sealed class DiscountGrpcTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private DiscountGrpcTestHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<DiscountGrpcTestHost> StartAsync(IDiscountRepository? repository = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        builder.Services.AddValidatorsFromAssembly(typeof(DiscountService).Assembly);
        builder.Services.AddSingleton<ValidationInterceptor>();
        builder.Services.AddSingleton(repository ?? Mock.Of<IDiscountRepository>());
        builder.Services.AddGrpc(options => options.Interceptors.Add<ValidationInterceptor>());

        var app = builder.Build();

        app.MapGrpcService<DiscountService>();
        app.MapHealthChecks("/health");

        await app.StartAsync();

        return new DiscountGrpcTestHost(app);
    }

    public static WebApplication BuildAppForRouteInspection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.Services.AddHealthChecks();
        builder.Services.AddValidatorsFromAssembly(typeof(DiscountService).Assembly);
        builder.Services.AddSingleton<ValidationInterceptor>();
        builder.Services.AddSingleton(Mock.Of<IDiscountRepository>());
        builder.Services.AddGrpc(options => options.Interceptors.Add<ValidationInterceptor>());

        var app = builder.Build();
        app.MapGrpcService<DiscountService>();
        app.MapHealthChecks("/health");

        return app;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        Client.Dispose();
    }
}
