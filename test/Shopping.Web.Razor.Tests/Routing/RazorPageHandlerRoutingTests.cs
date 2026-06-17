using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Routing;

public class RazorPageHandlerRoutingTests
{
    [Fact]
    public void RazorPages_RegisterExpectedRoutes()
    {
        using var factory = new ShoppingWebAppFactory();
        _ = factory.CreateClient();

        var endpointDisplayNames = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(x => x.DisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(endpointDisplayNames, x => x!.Contains("/Index", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointDisplayNames, x => x!.Contains("/ProductList", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointDisplayNames, x => x!.Contains("/ProductDetail", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointDisplayNames, x => x!.Contains("/Cart", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointDisplayNames, x => x!.Contains("/Checkout", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointDisplayNames, x => x!.Contains("/OrderList", StringComparison.OrdinalIgnoreCase));
    }
}
