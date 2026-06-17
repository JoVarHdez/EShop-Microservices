using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Data.Interceptors;
using Moq;
using Wolverine;

namespace Ordering.Tests.Unit.Infrastructure;

public class DispatchDomainEventsInterceptorTests
{
    [Fact]
    public async Task SavingChangesAsync_PublishesAndClearsDomainEvents()
    {
        var bus = new Mock<IMessageBus>();
        var publishedEvents = new List<object>();

        bus.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((message, _) => publishedEvents.Add(message))
            .Returns(ValueTask.CompletedTask);

        var setup = CreateDbContext(bus.Object);
        await using var dbContext = setup.DbContext;
        var order = CreateOrderWithDomainEvents(setup.CustomerId);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        Assert.Equal(2, publishedEvents.Count);
        Assert.Contains(publishedEvents, x => x.GetType().Name == "OrderCreatedEvent");
        Assert.Contains(publishedEvents, x => x.GetType().Name == "OrderUpdatedEvent");
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WithNoDomainEvents_DoesNotPublish()
    {
        var bus = new Mock<IMessageBus>();

        var setup = CreateDbContext(bus.Object);
        await using var dbContext = setup.DbContext;

        await dbContext.SaveChangesAsync();

        bus.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()), Times.Never);
    }

    private static (ApplicationDbContext DbContext, Guid CustomerId) CreateDbContext(IMessageBus bus)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new DispatchDomainEventsInterceptor(bus))
            .Options;

        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();

        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Interceptor Customer", "interceptor@example.com"));
        dbContext.SaveChanges();

        return (dbContext, customerId);
    }

    private static Order CreateOrderWithDomainEvents(Guid customerId)
    {
        var order = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerId),
            OrderName.Of("ORD-INTERCEPTOR"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("Sarah Connor", "4111111111111111", "12/30", "123", 1));

        order.Update(
            OrderName.Of("ORD-INTERCEPTOR-UPDATED"),
            Address.Of("Updated", "User", "updated@example.com", "Updated St", "US", "WA", "98052"),
            Address.Of("Updated", "User", "updated@example.com", "Updated St", "US", "WA", "98052"),
            Payment.Of("Updated User", "5555444433332222", "10/31", "999", 2),
            Ordering.Core.Enums.OrderStatus.Completed);

        return order;
    }
}