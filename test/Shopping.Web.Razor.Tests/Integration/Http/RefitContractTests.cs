using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Integration.Http;

public class RefitContractTests
{
    [Fact]
    public async Task CatalogService_GetProducts_UsesExpectedGetRoute()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, "{\"products\":[]}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);
        var sut = provider.GetRequiredService<ICatalogService>();

        _ = await sut.GetProductsAsync(2, 5);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/catalog-service/products?pageNumber=2&pageSize=5", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task BasketClient_GetBasket_UsesExpectedGetRoute()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, "{\"cart\":{\"userName\":\"swn\",\"items\":[]}}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);
        var sut = provider.GetRequiredService<IBasketApiClient>();

        _ = await sut.GetBasketAsync("swn");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/basket-service/basket/swn", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task BasketClient_StoreBasket_UsesExpectedPostRoute()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, "{\"userName\":\"swn\"}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);
        var sut = provider.GetRequiredService<IBasketApiClient>();

        _ = await sut.StoreBasketAsync(new StoreBasketRequest(TestData.Cart()));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/basket-service/basket", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task BasketClient_CheckoutBasket_UsesExpectedPostRoute()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, "{\"isSuccess\":true}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);
        var sut = provider.GetRequiredService<IBasketApiClient>();

        _ = await sut.CheckoutBasketAsync(new CheckoutBasketRequest(TestData.Checkout()));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/basket-service/basket/checkout", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task OrderingService_GetOrdersByCustomer_UsesExpectedGetRoute()
    {
        using var handler = new FakeHttpMessageHandler([
            JsonResponse(HttpStatusCode.OK, "{\"orders\":[]}")
        ]);
        using var provider = TestServiceProviderFactory.BuildWithApiClients(handler);
        var sut = provider.GetRequiredService<IOrderingService>();

        _ = await sut.GetOrdersByCustomerAsync(TestData.CustomerId);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"/ordering-service/orders/customer/{TestData.CustomerId}", request.RequestUri!.PathAndQuery);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
