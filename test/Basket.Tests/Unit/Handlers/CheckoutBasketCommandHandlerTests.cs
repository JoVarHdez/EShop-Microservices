using Basket.API.Basket.CheckoutBasket;
using Basket.API.Data;
using Basket.API.DTOs;
using Basket.API.Models;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using Moq;

namespace Basket.Tests.Unit.Handlers;

public class CheckoutBasketCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingBasket_PublishesEventAndDeletesBasket()
    {
        var repository = new Mock<IBasketRepository>();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var basket = new ShoppingCart("sarah")
        {
            Items =
            [
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Keyboard", Quantity = 1, Price = 100m },
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Mouse", Quantity = 2, Price = 50m }
            ]
        };

        repository.Setup(x => x.GetBasketAsync("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(basket);
        repository.Setup(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CheckoutBasketCommandHandler(repository.Object, publishEndpoint.Object);
        var command = new CheckoutBasketCommand(new BasketCheckoutDto
        {
            UserName = "sarah",
            CustomerId = Guid.NewGuid(),
            FirstName = "Sarah",
            LastName = "Connor",
            EmailAddress = "sarah@example.com",
            AddressLine = "Main St",
            Country = "US",
            State = "CA",
            ZipCode = "90001",
            CardName = "Sarah Connor",
            CardNumber = "4111111111111111",
            Expiration = "12/30",
            CVV = "123",
            PaymentMethod = 1
        });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        publishEndpoint.Verify(x => x.Publish(
            It.Is<BasketCheckoutEvent>(e =>
                e.UserName == "sarah" &&
                e.TotalPrice == 200m &&
                e.Items.Count == 2 &&
                e.Items.All(i => i.Quantity > 0)),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMissingBasket_ReturnsFalseAndDoesNotPublish()
    {
        var repository = new Mock<IBasketRepository>();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        repository.Setup(x => x.GetBasketAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ShoppingCart?)null);

        var handler = new CheckoutBasketCommandHandler(repository.Object, publishEndpoint.Object);

        var result = await handler.Handle(new CheckoutBasketCommand(new BasketCheckoutDto { UserName = "missing" }), CancellationToken.None);

        Assert.False(result.IsSuccess);
        publishEndpoint.Verify(x => x.Publish(It.IsAny<BasketCheckoutEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.DeleteBasketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
