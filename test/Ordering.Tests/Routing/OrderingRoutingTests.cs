using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Tests.Support;

namespace Ordering.Tests.Routing;

public class OrderingRoutingTests
{
    [Fact]
    public async Task MapOrdersEndpoints_RegistersExpectedRoutes()
    {
        await using var app = OrderingApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToHashSet();

        Assert.Contains("/orders/", routes);
        Assert.Contains("/orders/{orderName}", routes);
        Assert.Contains("/orders/customer/{customerId}", routes);
        Assert.Contains("/orders/{id}", routes);
        Assert.Contains("/health", routes);
    }

    [Fact]
    public async Task MapOrdersEndpoints_RegistersExpectedEndpointNames()
    {
        await using var app = OrderingApiTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var names = dataSource.Endpoints
            .Select(x => x.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName)
            .Where(x => x is not null)
            .ToHashSet();

        Assert.Contains("GetOrders", names);
        Assert.Contains("GetOrdersByName", names);
        Assert.Contains("GetOrdersByCustomer", names);
        Assert.Contains("CreateOrder", names);
        Assert.Contains("UpdateOrder", names);
        Assert.Contains("DeleteOrder", names);
    }
}