using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models.Catalog;
using Shopping.Web.Razor.Pages;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Unit.Pages;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsProductsAndReturnsPage()
    {
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductsAsync(It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new GetProductsResponse([TestData.Product()]));

        var basket = new Mock<IBasketService>();
        var sut = new IndexModel(catalog.Object, basket.Object, NullLogger<IndexModel>.Instance);

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.ProductList);
    }

    [Fact]
    public async Task OnPostAddToCartAsync_WhenProductMissing_ReturnsNotFound()
    {
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new GetProductByIdResponse(null!));

        var basket = new Mock<IBasketService>();
        var sut = new IndexModel(catalog.Object, basket.Object, NullLogger<IndexModel>.Instance);

        var result = await sut.OnPostAddToCartAsync(TestData.ProductId);

        Assert.IsType<NotFoundResult>(result);
        basket.Verify(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAddToCartAsync_WhenProductExists_StoresCartAndRedirects()
    {
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new GetProductByIdResponse(TestData.Product()));

        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(new ShoppingCartModel { UserName = "swn", Items = [] });
        basket.Setup(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>())).ReturnsAsync(new StoreBasketResponse("swn"));

        var sut = new IndexModel(catalog.Object, basket.Object, NullLogger<IndexModel>.Instance);

        var result = await sut.OnPostAddToCartAsync(TestData.ProductId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Cart", redirect.PageName);
        basket.Verify(x => x.StoreBasketAsync(It.Is<StoreBasketRequest>(r => r.Cart.Items.Count == 1)), Times.Once);
    }
}
