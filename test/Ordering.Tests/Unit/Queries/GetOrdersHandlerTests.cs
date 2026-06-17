using BuildingBlocks.Pagination;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Orders.Queries.GetOrders;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;

namespace Ordering.Tests.Unit.Queries;

public class GetOrdersHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithPagination_ReturnsExpectedPageAndCount()
    {
        await using var dbContext = CreateDbContext();
        SeedQueryData(dbContext);

        var handler = new GetOrdersHandler(dbContext);

        var result = await handler.HandleAsync(new PaginationRequest(PageIndex: 1, PageSize: 2), CancellationToken.None);

        Assert.Equal(1, result.Orders.PageIndex);
        Assert.Equal(2, result.Orders.PageSize);
        Assert.Equal(3, result.Orders.Count);

        var data = result.Orders.Data.ToList();
        Assert.Single(data);
        Assert.Equal("ORD-3", data[0].OrderName);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultPaginationRequest_ReturnsOrderedResults()
    {
        await using var dbContext = CreateDbContext();
        SeedQueryData(dbContext);

        var handler = new GetOrdersHandler(dbContext);

        var result = await handler.HandleAsync(new PaginationRequest(), CancellationToken.None);

        var names = result.Orders.Data.Select(x => x.OrderName).ToList();
        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], names);
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

    private static void SeedQueryData(ApplicationDbContext dbContext)
    {
        var customerId = Guid.NewGuid();
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var productCId = Guid.NewGuid();

        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Sarah Connor", "query-orders@example.com"));
        dbContext.Products.AddRange(
            Product.Create(ProductId.Of(productAId), "Keyboard", 100m),
            Product.Create(ProductId.Of(productBId), "Mouse", 60m),
            Product.Create(ProductId.Of(productCId), "Screen", 250m));

        var order1 = CreateOrder(customerId, "ORD-2", productAId, 1, 100m);
        var order2 = CreateOrder(customerId, "ORD-1", productBId, 1, 60m);
        var order3 = CreateOrder(customerId, "ORD-3", productCId, 1, 250m);

        dbContext.Orders.AddRange(order1, order2, order3);
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

        order.Update(order.OrderName, order.ShippingAddress, order.BillingAddress, order.Payment, OrderStatus.Pending);
        order.Add(ProductId.Of(productId), quantity, price);
        return order;
    }
}