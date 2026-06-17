using BuildingBlocks.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Application.Orders.EventHandlers.Integration;
using Ordering.Core.Enums;
using Wolverine;

namespace Ordering.Tests.Unit.EventHandlers;

public class BasketCheckoutEventHandlerTests
{
    [Fact]
    public async Task Consume_MapsCheckoutEventToCreateOrderCommand()
    {
        var capturedCommand = default(CreateOrderCommand);
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken, TimeSpan?>((message, _, _) => capturedCommand = Assert.IsType<CreateOrderCommand>(message))
            .Returns(Task.CompletedTask);

        var handler = new BasketCheckoutEventHandler(bus.Object, Mock.Of<ILogger<BasketCheckoutEventHandler>>());
        var checkoutEvent = CreateCheckoutEvent();
        var context = new Mock<ConsumeContext<BasketCheckoutEvent>>();
        context.SetupGet(x => x.Message).Returns(checkoutEvent);

        await handler.Consume(context.Object);

        Assert.NotNull(capturedCommand);
        Assert.Equal(checkoutEvent.UserName, capturedCommand!.Order.OrderName);
        Assert.Equal(checkoutEvent.CustomerId, capturedCommand.Order.CustomerId);
        Assert.Equal(OrderStatus.Pending, capturedCommand.Order.Status);
        Assert.Equal(checkoutEvent.EmailAddress, capturedCommand.Order.ShippingAddress.EmailAddress);
        Assert.Equal(checkoutEvent.CardNumber, capturedCommand.Order.Payment.CardNumber);
        Assert.Equal(2, capturedCommand.Order.OrderItems.Count);
        Assert.All(capturedCommand.Order.OrderItems, item => Assert.Equal(capturedCommand.Order.Id, item.OrderId));
        Assert.Equal(checkoutEvent.Items[0].ProductId, capturedCommand.Order.OrderItems[0].ProductId);
        Assert.Equal(checkoutEvent.Items[0].Quantity, capturedCommand.Order.OrderItems[0].Quantity);
        Assert.Equal(checkoutEvent.Items[0].UnitPrice, capturedCommand.Order.OrderItems[0].Price);
        bus.Verify(x => x.InvokeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BasketCheckoutEvent CreateCheckoutEvent()
    {
        return new BasketCheckoutEvent
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
            PaymentMethod = 1,
            Items =
            [
                new BasketCheckoutLineItem(Guid.NewGuid(), "Keyboard", 1, 100m),
                new BasketCheckoutLineItem(Guid.NewGuid(), "Mouse", 2, 30m)
            ]
        };
    }
}