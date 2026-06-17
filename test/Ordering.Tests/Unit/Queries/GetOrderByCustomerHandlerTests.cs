using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders.Queries.GetOrderByCustomer;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Queries;

public class GetOrderByCustomerHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithMatchingCustomer_ReturnsOnlyCustomerOrders()
    {
        await using var dbContext = CreateDbContext();
        var targetCustomerId = SeedCustomerQueryData(dbContext);
        var handler = new GetOrderByCustomerHandler(dbContext);

        var result = await handler.HandleAsync(targetCustomerId, CancellationToken.None);

        var orders = result.Orders.ToList();
        Assert.Equal(2, orders.Count);
        Assert.Equal(["CUST-ORD-1", "CUST-ORD-2"], orders.Select(x => x.OrderName).ToList());
        Assert.All(orders, o => Assert.Equal(targetCustomerId, o.CustomerId));
    }

    [Fact]
    public async Task HandleAsync_WithUnknownCustomer_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        SeedCustomerQueryData(dbContext);
        var handler = new GetOrderByCustomerHandler(dbContext);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Orders);
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

    private static Guid SeedCustomerQueryData(ApplicationDbContext dbContext)
    {
        var targetCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var productCId = Guid.NewGuid();

        dbContext.Customers.AddRange(
            Customer.Create(CustomerId.Of(targetCustomerId), "Target Customer", "target@example.com"),
            Customer.Create(CustomerId.Of(otherCustomerId), "Other Customer", "other@example.com"));

        dbContext.Products.AddRange(
            Product.Create(ProductId.Of(productAId), "Keyboard", 100m),
            Product.Create(ProductId.Of(productBId), "Mouse", 60m),
            Product.Create(ProductId.Of(productCId), "Headset", 75m));

        dbContext.Orders.AddRange(
            CreateOrder(targetCustomerId, "CUST-ORD-2", productAId, 1, 100m),
            CreateOrder(targetCustomerId, "CUST-ORD-1", productBId, 1, 60m),
            CreateOrder(otherCustomerId, "OTHER-ORD-1", productCId, 1, 75m));

        dbContext.SaveChanges();
        return targetCustomerId;
    }

    private static Order CreateOrder(Guid customerId, string orderName, Guid productId, int quantity, decimal price)
    {
        var order = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerId),
            OrderName.Of(orderName),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));

        order.Add(ProductId.Of(productId), quantity, price);
        return order;
    }
}