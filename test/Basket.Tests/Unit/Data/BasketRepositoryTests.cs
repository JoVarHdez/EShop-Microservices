using Basket.API.Data;
using Basket.API.Models;
using Marten;
using Moq;

namespace Basket.Tests.Unit.Data;

public class BasketRepositoryTests
{
    [Fact]
    public async Task GetBasketAsync_WhenBasketExists_ReturnsBasket()
    {
        var session = new Mock<IDocumentSession>();
        var basket = new ShoppingCart("sarah");
        session.Setup(x => x.LoadAsync<ShoppingCart>("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(basket);

        var repository = new BasketRepository(session.Object);

        var result = await repository.GetBasketAsync("sarah", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("sarah", result!.UserName);
    }

    [Fact]
    public async Task StoreBasketAsync_StoresAndSaves()
    {
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var basket = new ShoppingCart("sarah");

        var repository = new BasketRepository(session.Object);

        var result = await repository.StoreBasketAsync(basket, CancellationToken.None);

        Assert.Equal("sarah", result.UserName);
        session.Verify(x => x.Store(basket), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBasketAsync_DeletesAndSaves()
    {
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var repository = new BasketRepository(session.Object);

        var result = await repository.DeleteBasketAsync("sarah", CancellationToken.None);

        Assert.True(result);
        session.Verify(x => x.Delete<ShoppingCart>("sarah"), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
