using Ordering.Core.Enums;
using Ordering.Core.Events;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Tests.Unit.Domain;

public class OrderTests
{
    [Fact]
    public void Create_RaisesOrderCreatedEvent()
    {
        var order = CreateOrder();

        var createdEvent = Assert.Single(order.DomainEvents);
        Assert.IsType<OrderCreatedEvent>(createdEvent);
    }

    [Fact]
    public void Update_RaisesOrderUpdatedEventAndMutatesFields()
    {
        var order = CreateOrder();
        order.ClearDomainEvents();

        var updatedShipping = Address.Of("Updated", "User", "updated@example.com", "Updated St", "US", "WA", "98052");
        var updatedBilling = Address.Of("Updated", "User", "updated@example.com", "Updated Billing", "US", "WA", "98052");
        var updatedPayment = Payment.Of("Updated User", "5555444433332222", "10/31", "999", 2);

        order.Update(OrderName.Of("ORD-UPDATED"), updatedShipping, updatedBilling, updatedPayment, OrderStatus.Completed);

        Assert.Equal("ORD-UPDATED", order.OrderName.Value);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal("updated@example.com", order.ShippingAddress.EmailAddress);
        Assert.Equal("5555444433332222", order.Payment.CardNumber);

        var updatedEvent = Assert.Single(order.DomainEvents);
        Assert.IsType<OrderUpdatedEvent>(updatedEvent);
    }

    [Fact]
    public void Add_WithValidValues_AddsOrderItemAndUpdatesTotalAmount()
    {
        var order = CreateOrder();

        order.Add(ProductId.Of(Guid.NewGuid()), 2, 25m);
        order.Add(ProductId.Of(Guid.NewGuid()), 1, 10m);

        Assert.Equal(2, order.OrderItems.Count);
        Assert.Equal(60m, order.TotalAmount);
    }

    [Fact]
    public void Add_WithZeroOrNegativeQuantity_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(ProductId.Of(Guid.NewGuid()), 0, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(ProductId.Of(Guid.NewGuid()), -1, 10m));
    }

    [Fact]
    public void Add_WithZeroOrNegativePrice_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(ProductId.Of(Guid.NewGuid()), 1, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Add(ProductId.Of(Guid.NewGuid()), 1, -1m));
    }

    [Fact]
    public void Remove_WithExistingItem_RemovesItem()
    {
        var order = CreateOrder();
        order.Add(ProductId.Of(Guid.NewGuid()), 1, 10m);
        var itemId = order.OrderItems.Single().Id;

        order.Remove(itemId);

        Assert.Empty(order.OrderItems);
    }

    [Fact]
    public void Remove_WithUnknownItem_DoesNothing()
    {
        var order = CreateOrder();
        order.Add(ProductId.Of(Guid.NewGuid()), 1, 10m);

        order.Remove(OrderItemId.Of(Guid.NewGuid()));

        Assert.Single(order.OrderItems);
    }

    private static Order CreateOrder()
    {
        return Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(Guid.NewGuid()),
            OrderName.Of("ORD-DOMAIN"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));
    }
}