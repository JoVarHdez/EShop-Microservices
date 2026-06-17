using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.UpdateOrder;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Handlers;

public class UpdateOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithExistingOrder_UpdatesAndReturnsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var existingOrder = CreateExistingOrder();
        SeedCustomerAndProducts(dbContext, existingOrder.CustomerId.Value, existingOrder.OrderItems.Select(x => (x.ProductId.Value, x.Price)));
        dbContext.Orders.Add(existingOrder);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateOrderHandler(dbContext);
        var command = new UpdateOrderCommand(CreateOrderDto(existingOrder.Id.Value));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        var success = Assert.IsType<UpdateOrderResult>(result);
        Assert.True(success.IsSuccess);

        var storedOrder = await dbContext.Orders.SingleAsync();
        Assert.Equal("ORD-UPDATED", storedOrder.OrderName.Value);
        Assert.Equal(OrderStatus.Completed, storedOrder.Status);
        Assert.Equal("updated@example.com", storedOrder.ShippingAddress.EmailAddress);
        Assert.Equal("updated-billing@example.com", storedOrder.BillingAddress.EmailAddress);
        Assert.Equal("5555444433332222", storedOrder.Payment.CardNumber);
    }

    [Fact]
    public async Task HandleAsync_WithMissingOrder_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var handler = new UpdateOrderHandler(dbContext);

        var result = await handler.HandleAsync(
            new UpdateOrderCommand(CreateOrderDto(Guid.NewGuid())),
            CancellationToken.None);

        Assert.IsType<UpdateOrderNotFound>(result);
        Assert.Empty(dbContext.Orders);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private static Order CreateExistingOrder()
    {
        var order = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(Guid.NewGuid()),
            OrderName.Of("ORD-EXISTING"),
            Address.Of("Sarah", "Connor", "initial@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "initial-billing@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));

        order.Add(ProductId.Of(Guid.NewGuid()), 1, 50m);
        return order;
    }

    private static OrderDto CreateOrderDto(Guid id)
    {
        return new OrderDto(
            Id: id,
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-UPDATED",
            ShippingAddress: new AddressDto("Sarah", "Connor", "updated@example.com", "Updated St", "US", "WA", "98052"),
            BillingAddress: new AddressDto("Sarah", "Connor", "updated-billing@example.com", "Billing St", "US", "WA", "98053"),
            Payment: new PaymentDto("Sarah Connor", "5555444433332222", "10/31", "321", 2),
            Status: OrderStatus.Completed,
            OrderItems:
            [
                new OrderItemDto(id, Guid.NewGuid(), 1, 50m)
            ]);
    }

    private static void SeedCustomerAndProducts(ApplicationDbContext dbContext, Guid customerId, IEnumerable<(Guid ProductId, decimal Price)> products)
    {
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Sarah Connor", "initial@example.com"));
        dbContext.Products.AddRange(products.Select(product => Product.Create(ProductId.Of(product.ProductId), $"Product-{product.ProductId:N}", product.Price)));
        dbContext.SaveChanges();
    }
}