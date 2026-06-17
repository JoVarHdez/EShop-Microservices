using System.Net;
using System.Net.Http.Json;
using System.Text;
using Basket.API.Basket.CheckoutBasket;
using Basket.API.Basket.DeleteBasket;
using Basket.API.Basket.StoreBasket;
using Basket.API.Data;
using Basket.API.Models;
using Basket.Tests.Support;
using Moq;
using Wolverine;

namespace Basket.Tests.Integration.Endpoints;

public class BasketEndpointsTests
{
    [Fact]
    public async Task GetBasket_WithExistingUser_ReturnsOk()
    {
        var repository = new Mock<IBasketRepository>();
        repository.Setup(x => x.GetBasketAsync("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(new ShoppingCart("sarah"));

        var bus = new Mock<IMessageBus>();
        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.GetAsync("/basket/sarah");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBasket_WithMissingUser_ReturnsNotFound()
    {
        var repository = new Mock<IBasketRepository>();
        repository.Setup(x => x.GetBasketAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ShoppingCart?)null);

        var bus = new Mock<IMessageBus>();
        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.GetAsync("/basket/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StoreBasket_WithValidRequest_ReturnsCreated()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<StoreBasketResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreBasketResult("sarah"));

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.PostAsJsonAsync("/basket", new
        {
            cart = new
            {
                userName = "sarah",
                items = new[]
                {
                    new { productId = Guid.NewGuid(), productName = "Keyboard", quantity = 1, price = 100 }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/basket/sarah", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task StoreBasket_WithMalformedJson_ReturnsBadRequest()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.PostAsync("/basket", new StringContent("{", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBasket_WithValidUser_ReturnsOk()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<DeleteBasketResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteBasketResult(true));

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.DeleteAsync("/basket/sarah");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutBasket_WithSuccessResult_ReturnsOk()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<CheckoutBasketResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutBasketResult(true));

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.PostAsJsonAsync("/basket/checkout", new
        {
            basketCheckoutDto = new
            {
                userName = "sarah"
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutBasket_WithNotFoundResult_ReturnsNotFound()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<CheckoutBasketResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutBasketResult(false));

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.PostAsJsonAsync("/basket/checkout", new
        {
            basketCheckoutDto = new
            {
                userName = "missing"
            }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var repository = new Mock<IBasketRepository>();
        var bus = new Mock<IMessageBus>();

        await using var host = await BasketApiTestHost.StartAsync(bus.Object, repository.Object);

        var response = await host.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
