using Discount.Tests.Support;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Discount.Tests.Routing;

public class DiscountRoutingTests
{
    [Fact]
    public async Task DiscountGrpcService_AndHealth_Endpoints_AreRegistered()
    {
        await using var app = DiscountGrpcTestHost.BuildAppForRouteInspection();
        await app.StartAsync();

        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText)
            .ToHashSet();

        Assert.Contains("/health", routes);
        Assert.Contains(routes, route => route is not null && route.StartsWith("/discount.DiscountProtoService/", StringComparison.Ordinal));
    }
}
