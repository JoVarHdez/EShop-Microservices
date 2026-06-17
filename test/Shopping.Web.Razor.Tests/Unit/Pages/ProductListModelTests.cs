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

public class ProductListModelTests
{
    [Fact]
    public async Task OnGetAsync_WithCategory_LoadsCategoryProducts()
    {
        var categoryProducts = new[] { TestData.Product("Category Product") };
        var allProducts = new[] { TestData.Product(), TestData.Product("Laptop") };

        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductsByCategoryAsync("Smart Phone"))
            .ReturnsAsync(new GetProductsByCategoryResponse(categoryProducts));
        catalog.Setup(x => x.GetProductsAsync(It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new GetProductsResponse(allProducts));

        var basket = new Mock<IBasketService>();
        var sut = new ProductListModel(catalog.Object, basket.Object, NullLogger<ProductListModel>.Instance);

        var result = await sut.OnGetAsync("Smart Phone");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Smart Phone", sut.SelectedCategory);
        Assert.Single(sut.ProductList);
        Assert.NotEmpty(sut.CategoryList);
    }

    [Fact]
    public async Task OnGetAsync_WithoutCategory_LoadsAllProducts()
    {
        var products = new[] { TestData.Product(), TestData.Product("Laptop") };

        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductsAsync(It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new GetProductsResponse(products));

        var basket = new Mock<IBasketService>();
        var sut = new ProductListModel(catalog.Object, basket.Object, NullLogger<ProductListModel>.Instance);

        var result = await sut.OnGetAsync(string.Empty);

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, sut.ProductList.Count());
    }

    [Fact]
    public async Task OnPostAddToCartAsync_AddsItemAndRedirectsToCart()
    {
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(x => x.GetProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new GetProductByIdResponse(TestData.Product()));

        var basket = new Mock<IBasketService>();
        basket.Setup(x => x.LoadUserBasketAsync()).ReturnsAsync(new ShoppingCartModel { UserName = "swn", Items = [] });
        basket.Setup(x => x.StoreBasketAsync(It.IsAny<StoreBasketRequest>())).ReturnsAsync(new StoreBasketResponse("swn"));

        var sut = new ProductListModel(catalog.Object, basket.Object, NullLogger<ProductListModel>.Instance);

        var result = await sut.OnPostAddToCartAsync(TestData.ProductId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Cart", redirect.PageName);
        basket.Verify(x => x.StoreBasketAsync(It.Is<StoreBasketRequest>(r => r.Cart.Items.Count == 1)), Times.Once);
    }
}
