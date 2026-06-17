using Basket.Tests.Support;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Basket.Tests.Routing;

public class BasketRoutingTests
{
    [Fact]
    public async Task MapBasketEndpoints_RegistersExpectedRoutes()
    {
        await using var app = BasketApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToHashSet();

        Assert.Contains("/basket/{userName}", routes);
        Assert.Contains("/basket/", routes);
        Assert.Contains("/basket/checkout", routes);
        Assert.Contains("/health", routes);
    }

    [Fact]
    public async Task MapBasketEndpoints_RegistersExpectedEndpointNames()
    {
        await using var app = BasketApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var names = dataSource.Endpoints
            .Select(x => x.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName)
            .Where(x => x is not null)
            .ToHashSet();

        Assert.Contains("GetBasket", names);
        Assert.Contains("StoreBasket", names);
        Assert.Contains("DeleteBasket", names);
        Assert.Contains("CheckoutBasket", names);
    }
}
