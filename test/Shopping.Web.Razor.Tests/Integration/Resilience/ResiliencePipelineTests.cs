using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Integration.Resilience;

public class ResiliencePipelineTests
{
    [Fact]
    public async Task CatalogClient_RetriesOnTransientFailure()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"products\":[]}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);

        var catalog = provider.GetRequiredService<ICatalogService>();
        _ = await catalog.GetProductsAsync();

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task BasketClient_RetriesOnTransientFailure()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"cart\":{\"userName\":\"swn\",\"items\":[]}}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);

        var basket = provider.GetRequiredService<IBasketApiClient>();
        _ = await basket.GetBasketAsync("swn");

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task OrderingClient_RetriesOnTransientFailure()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"),
            JsonResponse(HttpStatusCode.OK, "{\"orders\":[]}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);

        var ordering = provider.GetRequiredService<IOrderingService>();
        _ = await ordering.GetOrdersByCustomerAsync(TestData.CustomerId);

        Assert.Equal(2, handler.RequestCount);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
