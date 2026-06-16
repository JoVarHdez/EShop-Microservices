using Catalog.Tests.Support;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Tests.Routing;

public class ProductsRoutingTests
{
    [Fact]
    public async Task MapProductsEndpoints_RegistersAllExpectedRoutes()
    {
        await using var app = ProductApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToHashSet();

        Assert.Contains("/products/", routes);
        Assert.Contains("/products/{id}", routes);
        Assert.Contains("/products/category/{categoryId}", routes);
    }

    [Fact]
    public async Task ProductsCategoryRoute_AndProductByIdRoute_AreBothRegistered()
    {
        await using var app = ProductApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routeNames = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName)
            .Where(x => x is not null)
            .ToHashSet();

        Assert.Contains("GetProductByCategory", routeNames);
        Assert.Contains("GetProductById", routeNames);
    }

    [Fact]
    public async Task HealthEndpoint_IsRegistered()
    {
        await using var app = ProductApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToHashSet();

        Assert.Contains("/health", routes);
    }
}
