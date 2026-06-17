using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders.Queries.GetOrderByName;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Queries;

public class GetOrdersByNameHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithSubstringFilter_ReturnsMatchingOrders()
    {
        await using var dbContext = CreateDbContext();
        SeedNameQueryData(dbContext);
        var handler = new GetOrdersByNameHandler(dbContext);

        var result = await handler.HandleAsync("ORD", CancellationToken.None);

        var names = result.Orders.Select(x => x.OrderName).ToList();
        Assert.Equal(["ORD-ALPHA", "ORD-BETA"], names);
    }

    [Fact]
    public async Task HandleAsync_WithNoMatch_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        SeedNameQueryData(dbContext);
        var handler = new GetOrdersByNameHandler(dbContext);

        var result = await handler.HandleAsync("MISSING", CancellationToken.None);

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

    private static void SeedNameQueryData(ApplicationDbContext dbContext)
    {
        var customerId = Guid.NewGuid();
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var productCId = Guid.NewGuid();

        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Name Query Customer", "name-query@example.com"));
        dbContext.Products.AddRange(
            Product.Create(ProductId.Of(productAId), "Keyboard", 100m),
            Product.Create(ProductId.Of(productBId), "Mouse", 60m),
            Product.Create(ProductId.Of(productCId), "Headset", 75m));

        dbContext.Orders.AddRange(
            CreateOrder(customerId, "ORD-BETA", productAId, 1, 100m),
            CreateOrder(customerId, "INV-100", productBId, 1, 60m),
            CreateOrder(customerId, "ORD-ALPHA", productCId, 1, 75m));

        dbContext.SaveChanges();
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