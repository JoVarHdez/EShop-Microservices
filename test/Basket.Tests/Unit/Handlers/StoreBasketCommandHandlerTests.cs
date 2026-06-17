using Basket.API.Basket.StoreBasket;
using Basket.API.Data;
using Basket.API.Models;
using Basket.Tests.Support.Fakes;
using Discount.Grpc;
using Moq;

namespace Basket.Tests.Unit.Handlers;

public class StoreBasketCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_StoresBasketAndReturnsUserName()
    {
        var repository = new Mock<IBasketRepository>();
        ShoppingCart? captured = null;
        repository.Setup(x => x.StoreBasketAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()))
            .Callback<ShoppingCart, CancellationToken>((cart, _) => captured = cart)
            .ReturnsAsync((ShoppingCart cart, CancellationToken _) => cart);

        var discountClient = new FakeDiscountProtoServiceClient(request => new CouponModel
        {
            Amount = request.ProductName == "Keyboard" ? 5 : 2
        });

        var handler = new StoreBasketCommandHandler(repository.Object, discountClient);

        var cart = new ShoppingCart("sarah")
        {
            Items =
            [
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Keyboard", Price = 100m, Quantity = 1 },
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Mouse", Price = 50m, Quantity = 2 }
            ]
        };

        var result = await handler.Handle(new StoreBasketCommand(cart), CancellationToken.None);

        Assert.Equal("sarah", result.UserName);
        Assert.NotNull(captured);
        Assert.Equal(95m, captured!.Items[0].Price);
        Assert.Equal(48m, captured.Items[1].Price);
        repository.Verify(x => x.StoreBasketAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
