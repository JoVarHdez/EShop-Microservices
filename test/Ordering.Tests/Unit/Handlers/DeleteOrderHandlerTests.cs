using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Ordering.Application.Orders.Commands.DeleteOrder;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Handlers;

public class DeleteOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithExistingOrder_RemovesOrderAndReturnsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var existingOrder = CreateExistingOrder();
        SeedCustomerAndProducts(dbContext, existingOrder.CustomerId.Value, existingOrder.OrderItems.Select(x => (x.ProductId.Value, x.Price)));
        dbContext.Orders.Add(existingOrder);
        await dbContext.SaveChangesAsync();

        var handler = new DeleteOrderHandler(dbContext);

        var result = await handler.HandleAsync(new DeleteOrderCommand(existingOrder.Id.Value), CancellationToken.None);

        var success = Assert.IsType<DeleteOrderResult>(result);
        Assert.True(success.IsSuccess);
        Assert.Empty(dbContext.Orders);
    }

    [Fact]
    public async Task HandleAsync_WithMissingOrder_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var handler = new DeleteOrderHandler(dbContext);

        var result = await handler.HandleAsync(new DeleteOrderCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<DeleteOrderNotFound>(result);
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
            OrderName.Of("ORD-DELETE"),
            Address.Of("Sarah", "Connor", "delete@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "delete@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));

        order.Add(ProductId.Of(Guid.NewGuid()), 1, 50m);
        return order;
    }

    private static void SeedCustomerAndProducts(ApplicationDbContext dbContext, Guid customerId, IEnumerable<(Guid ProductId, decimal Price)> products)
    {
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Sarah Connor", "delete@example.com"));
        dbContext.Products.AddRange(products.Select(product => Product.Create(ProductId.Of(product.ProductId), $"Product-{product.ProductId:N}", product.Price)));
        dbContext.SaveChanges();
    }
}