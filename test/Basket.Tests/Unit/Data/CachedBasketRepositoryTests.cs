using System.Text;
using System.Text.Json;
using Basket.API.Data;
using Basket.API.Models;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace Basket.Tests.Unit.Data;

public class CachedBasketRepositoryTests
{
    [Fact]
    public async Task GetBasketAsync_WhenCacheHit_ReturnsCachedBasket()
    {
        var userName = "sarah";
        var cachedBasket = new ShoppingCart(userName);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedBasket));

        var inner = new Mock<IBasketRepository>();
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync(userName, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        var repository = new CachedBasketRepository(inner.Object, cache.Object);

        var result = await repository.GetBasketAsync(userName, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userName, result!.UserName);
        inner.Verify(x => x.GetBasketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBasketAsync_WhenCacheMiss_LoadsFromInnerAndCaches()
    {
        var userName = "sarah";
        var basket = new ShoppingCart(userName);

        var inner = new Mock<IBasketRepository>();
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync(userName, It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        inner.Setup(x => x.GetBasketAsync(userName, It.IsAny<CancellationToken>())).ReturnsAsync(basket);

        var repository = new CachedBasketRepository(inner.Object, cache.Object);

        var result = await repository.GetBasketAsync(userName, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userName, result!.UserName);
        cache.Verify(x => x.SetAsync(userName, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreBasketAsync_StoresInnerAndCaches()
    {
        var basket = new ShoppingCart("sarah");

        var inner = new Mock<IBasketRepository>();
        var cache = new Mock<IDistributedCache>();
        inner.Setup(x => x.StoreBasketAsync(basket, It.IsAny<CancellationToken>())).ReturnsAsync(basket);

        var repository = new CachedBasketRepository(inner.Object, cache.Object);

        var result = await repository.StoreBasketAsync(basket, CancellationToken.None);

        Assert.Equal("sarah", result.UserName);
        inner.Verify(x => x.StoreBasketAsync(basket, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.SetAsync("sarah", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBasketAsync_DeletesInnerAndRemovesCache()
    {
        var inner = new Mock<IBasketRepository>();
        var cache = new Mock<IDistributedCache>();
        inner.Setup(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var repository = new CachedBasketRepository(inner.Object, cache.Object);

        var result = await repository.DeleteBasketAsync("sarah", CancellationToken.None);

        Assert.True(result);
        inner.Verify(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync("sarah", It.IsAny<CancellationToken>()), Times.Once);
    }
}
