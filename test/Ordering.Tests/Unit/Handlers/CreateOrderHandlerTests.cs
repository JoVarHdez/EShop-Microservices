using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Handlers;

public class CreateOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidOrder_PersistsAggregateAndReturnsId()
    {
        await using var dbContext = CreateDbContext();
        var command = new CreateOrderCommand(CreateOrderDto());
        SeedCustomerAndProducts(dbContext, command.Order.CustomerId, command.Order.OrderItems);

        var handler = new CreateOrderHandler(dbContext);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);

        var storedOrder = await dbContext.Orders.Include(x => x.OrderItems).SingleAsync();
        Assert.Equal(result.Id, storedOrder.Id.Value);
        Assert.Equal(command.Order.CustomerId, storedOrder.CustomerId.Value);
        Assert.Equal(command.Order.OrderName, storedOrder.OrderName.Value);
        Assert.Equal(command.Order.ShippingAddress.EmailAddress, storedOrder.ShippingAddress.EmailAddress);
        Assert.Equal(command.Order.BillingAddress.EmailAddress, storedOrder.BillingAddress.EmailAddress);
        Assert.Equal(command.Order.Payment.CardNumber, storedOrder.Payment.CardNumber);
        Assert.Equal(2, storedOrder.OrderItems.Count);
        Assert.Equal(80m, storedOrder.TotalAmount);
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

    private static OrderDto CreateOrderDto()
    {
        return new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-CREATE",
            ShippingAddress: new AddressDto("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("Sarah", "Connor", "billing@example.com", "Second St", "US", "CA", "90002"),
            Payment: new PaymentDto("Sarah Connor", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 1, 50m),
                new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 2, 15m)
            ]);
    }

    private static void SeedCustomerAndProducts(ApplicationDbContext dbContext, Guid customerId, IEnumerable<OrderItemDto> items)
    {
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Sarah Connor", "sarah@example.com"));
        dbContext.Products.AddRange(items.Select(item => Product.Create(ProductId.Of(item.ProductId), $"Product-{item.ProductId:N}", item.Price)));
        dbContext.SaveChanges();
    }
}