using System.Net;
using System.Net.Http.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ordering.API.Endpoints;
using Ordering.Application.DTOs;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;
using Ordering.Tests.Support;

namespace Ordering.Tests.Integration.Endpoints;

[Collection("OrderingSqlServer")]
public class OrderingEndpointsWithSqlServerTests
{
    private readonly OrderingSqlServerFixture _fixture;

    public OrderingEndpointsWithSqlServerTests(OrderingSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrders_WithSqlServer_ReturnsSeededOrdersAndCount()
    {
        await _fixture.ResetAsync();
        await _fixture.SeedInitialOrdersAsync();

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);

        var response = await host.Client.GetAsync("/orders?pageIndex=0&pageSize=10");
        var payload = await response.Content.ReadFromJsonAsync<GetOrdersResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Orders.Count >= 2);
        Assert.True(payload.Orders.Data.Any());
    }

    [Fact]
    public async Task GetOrdersByName_WithSqlServer_UsesSubstringFiltering()
    {
        await _fixture.ResetAsync();
        await _fixture.SeedInitialOrdersAsync();

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);

        var response = await host.Client.GetAsync("/orders/ORD");
        var payload = await response.Content.ReadFromJsonAsync<GetOrdersByNameResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Orders);
    }

    [Fact]
    public async Task CreateOrder_WithSqlServer_PersistsOrderAndItems()
    {
        await _fixture.ResetAsync();
        var customerId = await _fixture.EnsureDefaultCustomerAsync();
        var productId = await _fixture.EnsureDefaultProductAsync();

        var request = new CreateOrderRequest(new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: customerId,
            OrderName: "ORD-SQL-CREATE",
            ShippingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment: new PaymentDto("John Doe", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), productId, 2, 500m)
            ]));

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);
        var response = await host.Client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var dbContext = await _fixture.CreateDbContextAsync();
        var storedOrder = await dbContext.Orders
            .Include(x => x.OrderItems)
            .SingleOrDefaultAsync(x => x.OrderName == OrderName.Of("ORD-SQL-CREATE"));

        Assert.NotNull(storedOrder);
        Assert.Equal(customerId, storedOrder.CustomerId.Value);
        Assert.Single(storedOrder.OrderItems);
        Assert.Equal(1000m, storedOrder.TotalAmount);
    }

    [Fact]
    public async Task UpdateOrder_WithSqlServer_UpdatesStatus()
    {
        await _fixture.ResetAsync();
        var customerId = await _fixture.EnsureDefaultCustomerAsync();
        var productId = await _fixture.EnsureDefaultProductAsync();

        var existingOrder = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerId),
            OrderName.Of("ORD-SQL-UPDATE"),
            Address.Of("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("John Doe", "4111111111111111", "12/30", "123", 1));
        existingOrder.Add(ProductId.Of(productId), 1, 500m);

        await _fixture.SeedAsync(existingOrder);

        var request = new UpdateOrderRequest(new OrderDto(
            Id: existingOrder.Id.Value,
            CustomerId: customerId,
            OrderName: "ORD-SQL-UPDATE",
            ShippingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment: new PaymentDto("John Doe", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Completed,
            OrderItems:
            [
                new OrderItemDto(existingOrder.Id.Value, productId, 1, 500m)
            ]));

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);
        var response = await host.Client.PutAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var dbContext = await _fixture.CreateDbContextAsync();
        var storedOrder = await dbContext.Orders.SingleAsync(x => x.Id == existingOrder.Id);
        Assert.Equal(OrderStatus.Completed, storedOrder.Status);
    }

    [Fact]
    public async Task DeleteOrder_WithSqlServer_RemovesOrder()
    {
        await _fixture.ResetAsync();
        var customerId = await _fixture.EnsureDefaultCustomerAsync();
        var productId = await _fixture.EnsureDefaultProductAsync();

        var existingOrder = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerId),
            OrderName.Of("ORD-SQL-DELETE"),
            Address.Of("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("John Doe", "4111111111111111", "12/30", "123", 1));
        existingOrder.Add(ProductId.Of(productId), 1, 500m);

        await _fixture.SeedAsync(existingOrder);

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);
        var response = await host.Client.DeleteAsync($"/orders/{existingOrder.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var dbContext = await _fixture.CreateDbContextAsync();
        var storedOrder = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == existingOrder.Id);
        Assert.Null(storedOrder);
    }

    [Fact]
    public async Task Health_WithSqlServer_ReturnsOk()
    {
        await _fixture.ResetAsync();

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(_fixture.ConnectionString);
        var response = await host.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithSqlServerAndFeatureEnabled_PublishesOrderDto()
    {
        await _fixture.ResetAsync();
        var customerId = await _fixture.EnsureDefaultCustomerAsync();
        var productId = await _fixture.EnsureDefaultProductAsync();

        var publishEndpoint = new Mock<IPublishEndpoint>();

        var request = new CreateOrderRequest(new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: customerId,
            OrderName: "ORD-SQL-PUBLISH-ON",
            ShippingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment: new PaymentDto("John Doe", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), productId, 1, 500m)
            ]));

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(
            _fixture.ConnectionString,
            publishEndpoint: publishEndpoint.Object,
            orderFulfilmentEnabled: true);

        var response = await host.Client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        publishEndpoint.Verify(x => x.Publish(
            It.Is<OrderDto>(dto =>
                dto.OrderName == request.Order.OrderName &&
                dto.CustomerId == request.Order.CustomerId &&
                dto.OrderItems.Count == request.Order.OrderItems.Count),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WithSqlServerAndFeatureDisabled_DoesNotPublishOrderDto()
    {
        await _fixture.ResetAsync();
        var customerId = await _fixture.EnsureDefaultCustomerAsync();
        var productId = await _fixture.EnsureDefaultProductAsync();

        var publishEndpoint = new Mock<IPublishEndpoint>();

        var request = new CreateOrderRequest(new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: customerId,
            OrderName: "ORD-SQL-PUBLISH-OFF",
            ShippingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("John", "Doe", "john.doe@example.com", "Main St", "US", "CA", "90001"),
            Payment: new PaymentDto("John Doe", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), productId, 1, 500m)
            ]));

        await using var host = await OrderingApiTestHost.StartWithSqlServerAsync(
            _fixture.ConnectionString,
            publishEndpoint: publishEndpoint.Object,
            orderFulfilmentEnabled: false);

        var response = await host.Client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        publishEndpoint.Verify(x => x.Publish(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}