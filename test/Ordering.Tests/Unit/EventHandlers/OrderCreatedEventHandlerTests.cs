using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.EventHandlers.Domain;
using Ordering.Core.Events;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Tests.Unit.EventHandlers;

public class OrderCreatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WithFeatureEnabled_PublishesMappedOrderDto()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabledAsync("OrderFullfilment")).ReturnsAsync(true);

        var handler = new OrderCreatedEventHandler(
            publishEndpoint.Object,
            featureManager.Object,
            Mock.Of<ILogger<OrderCreatedEventHandler>>());

        var order = CreateOrder();

        await handler.Handle(new OrderCreatedEvent(order), CancellationToken.None);

        publishEndpoint.Verify(x => x.Publish(
            It.Is<OrderDto>(dto =>
                dto.Id == order.Id.Value &&
                dto.CustomerId == order.CustomerId.Value &&
                dto.OrderName == order.OrderName.Value &&
                dto.OrderItems.Count == 1 &&
                dto.OrderItems[0].ProductId == order.OrderItems[0].ProductId.Value),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFeatureDisabled_DoesNotPublish()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabledAsync("OrderFullfilment")).ReturnsAsync(false);

        var handler = new OrderCreatedEventHandler(
            publishEndpoint.Object,
            featureManager.Object,
            Mock.Of<ILogger<OrderCreatedEventHandler>>());

        await handler.Handle(new OrderCreatedEvent(CreateOrder()), CancellationToken.None);

        publishEndpoint.Verify(x => x.Publish(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Order CreateOrder()
    {
        var order = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(Guid.NewGuid()),
            OrderName.Of("ORD-EVENT"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));

        order.Add(ProductId.Of(Guid.NewGuid()), 1, 100m);
        return order;
    }
}