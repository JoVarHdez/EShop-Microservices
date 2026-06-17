using Basket.API.Basket;
using Basket.API.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Wolverine;

namespace Basket.Tests.Support;

public sealed class BasketApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private BasketApiTestHost(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<BasketApiTestHost> StartAsync(IMessageBus messageBus, IBasketRepository basketRepository)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(messageBus);
        builder.Services.AddSingleton(basketRepository);

        var app = builder.Build();
        app.MapBasketEndpoints();
        app.MapHealthChecks("/health");

        await app.StartAsync();

        return new BasketApiTestHost(app, app.GetTestClient());
    }

    public static WebApplication BuildAppForRouteInspection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(Mock.Of<IMessageBus>());
        builder.Services.AddSingleton(Mock.Of<IBasketRepository>());

        var app = builder.Build();
        app.MapBasketEndpoints();
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
