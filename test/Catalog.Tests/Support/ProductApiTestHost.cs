using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.DeleteProduct;
using Catalog.API.Products.GetProductByCategory;
using Catalog.API.Products.GetProductById;
using Catalog.API.Products.GetProducts;
using Catalog.API.Products.UpdateProduct;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Wolverine;

namespace Catalog.Tests.Support;

public sealed class ProductApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ProductApiTestHost(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<ProductApiTestHost> StartAsync(IMessageBus messageBus, IQuerySession? querySession = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(querySession ?? Mock.Of<IQuerySession>());
        builder.Services.AddSingleton(messageBus);

        var app = builder.Build();

        var group = app.MapGroup("/products");
        group.MapCreateProductEndpoint();
        group.MapGetProductsEndpoint();
        group.MapGetProductByIdEndpoint();
        group.MapGetProductByCategoryEndpoint();
        group.MapUpdateProductEndpoint();
        group.MapDeleteProductEndpoint();

        app.MapHealthChecks("/health");

        await app.StartAsync();

        return new ProductApiTestHost(app, app.GetTestClient());
    }

    public static async Task<ProductApiTestHost> StartWithMartenAsync(string connectionString, IMessageBus messageBus)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        builder.Services.AddMarten(opts =>
        {
            opts.Connection(connectionString);
        }).UseLightweightSessions();

        builder.Services.AddSingleton(messageBus);

        var app = builder.Build();

        var group = app.MapGroup("/products");
        group.MapCreateProductEndpoint();
        group.MapGetProductsEndpoint();
        group.MapGetProductByIdEndpoint();
        group.MapGetProductByCategoryEndpoint();
        group.MapUpdateProductEndpoint();
        group.MapDeleteProductEndpoint();

        app.MapHealthChecks("/health");

        await app.StartAsync();

        return new ProductApiTestHost(app, app.GetTestClient());
    }

    public static WebApplication BuildAppForRouteInspection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(Mock.Of<IQuerySession>());
        builder.Services.AddSingleton(Mock.Of<IMessageBus>());

        var app = builder.Build();
        var group = app.MapGroup("/products");
        group.MapCreateProductEndpoint();
        group.MapGetProductsEndpoint();
        group.MapGetProductByIdEndpoint();
        group.MapGetProductByCategoryEndpoint();
        group.MapUpdateProductEndpoint();
        group.MapDeleteProductEndpoint();
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
