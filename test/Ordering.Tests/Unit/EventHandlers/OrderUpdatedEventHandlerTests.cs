using Microsoft.Extensions.Logging;
using Moq;
using Ordering.Application.Orders.EventHandlers.Domain;
using Ordering.Core.Events;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Tests.Unit.EventHandlers;

public class OrderUpdatedEventHandlerTests
{
    [Fact]
    public async Task Handle_CompletesWithoutThrowing()
    {
        var handler = new OrderUpdatedEventHandler(Mock.Of<ILogger<OrderUpdatedEventHandler>>());

        var exception = await Record.ExceptionAsync(() => handler.Handle(new OrderUpdatedEvent(CreateOrder()), CancellationToken.None));

        Assert.Null(exception);
    }

    private static Order CreateOrder()
    {
        return Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(Guid.NewGuid()),
            OrderName.Of("ORD-UPDATED-EVENT"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));
    }
}