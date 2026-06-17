using Microsoft.Extensions.DependencyInjection;
using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Integration.Startup;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddApiClients_RegistersRefitClients_AndBasketService()
    {
        using var handler = new FakeHttpMessageHandler([]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);

        var catalog = provider.GetRequiredService<ICatalogService>();
        var basketApi = provider.GetRequiredService<IBasketApiClient>();
        var ordering = provider.GetRequiredService<IOrderingService>();
        var basketService = provider.GetRequiredService<IBasketService>();

        Assert.NotNull(catalog);
        Assert.NotNull(basketApi);
        Assert.NotNull(ordering);
        Assert.NotNull(basketService);
        Assert.IsType<BasketService>(basketService);
    }

    [Fact]
    public void OptionsBinding_ProvidesConfiguredGatewayAddress()
    {
        using var handler = new FakeHttpMessageHandler([]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value;

        Assert.Equal("https://example.test", options.GatewayAddress);
    }
}
