using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Pages;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Unit.Pages;

public class CartModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsCartAndReturnsPage()
    {
        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(TestData.Cart());

        var sut = new CartModel(basket.Object, NullLogger<CartModel>.Instance);

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("swn", sut.Cart.UserName);
    }

    [Fact]
    public async Task OnPostRemoveToCartAsync_RemovesItemAndPersists()
    {
        var cart = TestData.Cart();

        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(cart);
        basket.Setup(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>())).ReturnsAsync(new StoreBasketResponse("swn"));

        var sut = new CartModel(basket.Object, NullLogger<CartModel>.Instance);

        var result = await sut.OnPostRemoveToCartAsync(TestData.ProductId);

        Assert.IsType<RedirectToPageResult>(result);
        basket.Verify(x => x.StoreBasketAsync(It.Is<StoreBasketRequest>(r => r.Cart.Items.Count == 0)), Times.Once);
    }
}
