using Moq;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Unit.Services;

public class BasketServiceTests
{
    [Fact]
    public async Task LoadUserBasketAsync_WhenApiSucceeds_ReturnsCart()
    {
        var expected = TestData.Cart();
        var client = new Mock<IBasketApiClient>();
        client.Setup(x => x.GetBasketAsync("swn")).ReturnsAsync(new GetBasketResponse(expected));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new BasketService(client.Object, user.Object);

        var result = await sut.LoadUserBasketAsync();

        Assert.Equal("swn", result.UserName);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task LoadUserBasketAsync_WhenApiThrows_ReturnsEmptyUserCart()
    {
        var client = new Mock<IBasketApiClient>();
        client.Setup(x => x.GetBasketAsync("swn")).ThrowsAsync(new HttpRequestException("boom"));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new BasketService(client.Object, user.Object);

        var result = await sut.LoadUserBasketAsync();

        Assert.Equal("swn", result.UserName);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task StoreBasketAsync_DelegatesToApiClient()
    {
        var cart = TestData.Cart();
        var client = new Mock<IBasketApiClient>();
        client.Setup(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>()))
            .ReturnsAsync(new StoreBasketResponse("swn"));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new BasketService(client.Object, user.Object);

        var response = await sut.StoreBasketAsync(new StoreBasketRequest(cart));

        Assert.Equal("swn", response.UserName);
        client.Verify(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBasketAsync_DelegatesToApiClient()
    {
        var client = new Mock<IBasketApiClient>();
        client.Setup(x => x.DeleteBasketAsync("swn")).ReturnsAsync(new DeleteBasketResponse(true));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new BasketService(client.Object, user.Object);

        var response = await sut.DeleteBasketAsync("swn");

        Assert.True(response.IsSuccess);
        client.Verify(x => x.DeleteBasketAsync("swn"), Times.Once);
    }

    [Fact]
    public async Task CheckoutBasketAsync_DelegatesToApiClient()
    {
        var client = new Mock<IBasketApiClient>();
        client.Setup(x => x.CheckoutBasketAsync(It.IsAny<CheckoutBasketRequest>()))
            .ReturnsAsync(new CheckoutBasketResponse(true));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new BasketService(client.Object, user.Object);

        var response = await sut.CheckoutBasketAsync(new CheckoutBasketRequest(TestData.Checkout()));

        Assert.True(response.IsSuccess);
        client.Verify(x => x.CheckoutBasketAsync(It.IsAny<CheckoutBasketRequest>()), Times.Once);
    }
}
