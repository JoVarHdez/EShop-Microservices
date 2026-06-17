using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ordering.API.Endpoints;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.DeleteOrder;
using Ordering.Application.Orders.Commands.UpdateOrder;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;
using Ordering.Infrastructure.Data;
using Ordering.Tests.Support;
using Wolverine;

namespace Ordering.Tests.Integration.Endpoints;

public class OrderingEndpointsTests
{
    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreated()
    {
        var request = CreateCreateOrderRequest();
        await using var host = await OrderingApiTestHost.StartAsync(db => SeedCustomerAndProducts(db, request.Order.CustomerId, request.Order.OrderItems));

        var response = await host.Client.PostAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateOrder_WithMalformedJson_ReturnsBadRequest()
    {
        await using var host = await OrderingApiTestHost.StartAsync();

        var response = await host.Client.PostAsync("/orders", new StringContent("{", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_WithMissingOrder_ReturnsNotFound()
    {
        var request = CreateUpdateOrderRequest(Guid.NewGuid());
        await using var host = await OrderingApiTestHost.StartAsync();

        var response = await host.Client.PutAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_WithMalformedGuid_ReturnsBadRequest()
    {
        await using var host = await OrderingApiTestHost.StartAsync();

        var response = await host.Client.DeleteAsync("/orders/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithoutQueryParameters_UsesDefaultPagination()
    {
        await using var host = await OrderingApiTestHost.StartAsync(SeedOrders);

        var response = await host.Client.GetAsync("/orders");
        var payload = await response.Content.ReadFromJsonAsync<GetOrdersResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(0, payload.Orders.PageIndex);
        Assert.Equal(10, payload.Orders.PageSize);
        Assert.Equal(2, payload.Orders.Count);
        Assert.Equal(2, payload.Orders.Data.Count());
    }

    [Fact]
    public async Task GetOrdersByName_WithNoMatches_ReturnsOkWithEmptyCollection()
    {
        await using var host = await OrderingApiTestHost.StartAsync(SeedOrders);

        var response = await host.Client.GetAsync("/orders/does-not-exist");
        var payload = await response.Content.ReadFromJsonAsync<GetOrdersByNameResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Empty(payload.Orders);
    }

    [Fact]
    public async Task GetOrdersByCustomer_WithNoMatches_ReturnsOkWithEmptyCollection()
    {
        await using var host = await OrderingApiTestHost.StartAsync(SeedOrders);

        var response = await host.Client.GetAsync($"/orders/customer/{Guid.NewGuid()}");
        var payload = await response.Content.ReadFromJsonAsync<GetOrdersByCustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Empty(payload.Orders);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        await using var host = await OrderingApiTestHost.StartAsync();

        var response = await host.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrder_WhenCommandReturnsUnexpectedResult_ReturnsInternalServerError()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<UpdateOrderCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnknownUpdateOrderResult());

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(bus.Object);

        await using var app = builder.Build();
        app.MapGroup("/orders").MapUpdateOrder();
        await app.StartAsync();

        var request = CreateUpdateOrderRequest(Guid.NewGuid());
        var response = await app.GetTestClient().PutAsJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_WhenCommandReturnsUnexpectedResult_ReturnsInternalServerError()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<DeleteOrderCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnknownDeleteOrderResult());

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(bus.Object);

        await using var app = builder.Build();
        app.MapGroup("/orders").MapDeleteOrder();
        await app.StartAsync();

        var response = await app.GetTestClient().DeleteAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private sealed record UnknownUpdateOrderResult : UpdateOrderCommandResult;

    private sealed record UnknownDeleteOrderResult : DeleteOrderCommandResult;

    private static CreateOrderRequest CreateCreateOrderRequest()
    {
        return new CreateOrderRequest(new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-ENDPOINT-CREATE",
            ShippingAddress: new AddressDto("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("Sarah", "Connor", "billing@example.com", "Billing St", "US", "CA", "90002"),
            Payment: new PaymentDto("Sarah Connor", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 1, 50m)
            ]));
    }

    private static UpdateOrderRequest CreateUpdateOrderRequest(Guid id)
    {
        return new UpdateOrderRequest(new OrderDto(
            Id: id,
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-ENDPOINT-UPDATE",
            ShippingAddress: new AddressDto("Sarah", "Connor", "update@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("Sarah", "Connor", "update-billing@example.com", "Billing St", "US", "CA", "90002"),
            Payment: new PaymentDto("Sarah Connor", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Completed,
            OrderItems:
            [
                new OrderItemDto(id, Guid.NewGuid(), 1, 50m)
            ]));
    }

    private static void SeedOrders(ApplicationDbContext dbContext)
    {
        var customerOneId = Guid.NewGuid();
        var customerTwoId = Guid.NewGuid();
        var productOneId = Guid.NewGuid();
        var productTwoId = Guid.NewGuid();

        dbContext.Customers.AddRange(
            Customer.Create(CustomerId.Of(customerOneId), "John Doe", "john@example.com"),
            Customer.Create(CustomerId.Of(customerTwoId), "Jane Smith", "jane@example.com"));

        dbContext.Products.AddRange(
            Product.Create(ProductId.Of(productOneId), "Keyboard", 50m),
            Product.Create(ProductId.Of(productTwoId), "Mouse", 30m));

        var firstOrder = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerOneId),
            OrderName.Of("ORD-1"),
            Address.Of("John", "Doe", "john@example.com", "Main St", "US", "CA", "90001"),
            Address.Of("John", "Doe", "john@example.com", "Main St", "US", "CA", "90001"),
            Payment.Of("John Doe", "4111111111111111", "12/30", "123", 1));
        firstOrder.Add(ProductId.Of(productOneId), 1, 50m);

        var secondOrder = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(customerTwoId),
            OrderName.Of("ORD-2"),
            Address.Of("Jane", "Smith", "jane@example.com", "Second St", "US", "WA", "98052"),
            Address.Of("Jane", "Smith", "jane@example.com", "Second St", "US", "WA", "98052"),
            Payment.Of("Jane Smith", "5555444433332222", "11/31", "321", 2));
        secondOrder.Add(ProductId.Of(productTwoId), 1, 30m);

        dbContext.Orders.AddRange(firstOrder, secondOrder);
    }

    private static void SeedCustomerAndProducts(ApplicationDbContext dbContext, Guid customerId, IEnumerable<OrderItemDto> items)
    {
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(customerId), "Sarah Connor", "sarah@example.com"));
        dbContext.Products.AddRange(items.Select(item => Product.Create(ProductId.Of(item.ProductId), $"Product-{item.ProductId:N}", item.Price)));
    }
}