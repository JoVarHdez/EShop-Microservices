using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Pages;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Unit.Pages;

public class CheckoutModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsCartAndReturnsPage()
    {
        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(TestData.Cart());

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new CheckoutModel(basket.Object, user.Object, NullLogger<CheckoutModel>.Instance)
        {
            Order = TestData.Checkout()
        };

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("swn", sut.Cart.UserName);
    }

    [Fact]
    public async Task OnPostCheckOutAsync_WithInvalidModelState_ReturnsPageAndDoesNotCheckout()
    {
        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(TestData.Cart());

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var sut = new CheckoutModel(basket.Object, user.Object, NullLogger<CheckoutModel>.Instance)
        {
            Order = TestData.Checkout()
        };
        sut.ModelState.AddModelError("Order", "Invalid");

        var result = await sut.OnPostCheckOutAsync();

        Assert.IsType<PageResult>(result);
        basket.Verify(x => x.CheckoutBasketAsync(It.IsAny<CheckoutBasketRequest>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCheckOutAsync_WithValidModelState_MapsOrderAndRedirects()
    {
        var cart = TestData.Cart();

        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(cart);
        basket.Setup(x => x.CheckoutBasketAsync(It.IsAny<CheckoutBasketRequest>()))
            .ReturnsAsync(new CheckoutBasketResponse(true));

        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var order = TestData.Checkout();
        var sut = new CheckoutModel(basket.Object, user.Object, NullLogger<CheckoutModel>.Instance)
        {
            Order = order
        };

        var result = await sut.OnPostCheckOutAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Confirmation", redirect.PageName);
        Assert.Equal("OrderSubmitted", redirect.PageHandler);

        basket.Verify(x => x.CheckoutBasketAsync(It.Is<CheckoutBasketRequest>(r =>
            r.BasketCheckoutDto.UserName == "swn" &&
            r.BasketCheckoutDto.CustomerId == TestData.CustomerId &&
            r.BasketCheckoutDto.TotalPrice == cart.TotalPrice)), Times.Once);
    }
}
