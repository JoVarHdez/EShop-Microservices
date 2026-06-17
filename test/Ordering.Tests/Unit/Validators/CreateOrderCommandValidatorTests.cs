using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Core.Enums;

namespace Ordering.Tests.Unit.Validators;

public class CreateOrderCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new CreateOrderCommandValidator();

        var result = validator.Validate(new CreateOrderCommand(CreateValidOrderDto()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyOrderName_Fails()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(CreateValidOrderDto() with { OrderName = string.Empty });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Order.OrderName");
    }

    [Fact]
    public void Validate_WithEmptyOrderItems_Fails()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(CreateValidOrderDto() with { OrderItems = [] });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Order.OrderItems");
    }

    private static OrderDto CreateValidOrderDto()
    {
        return new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-100",
            ShippingAddress: new AddressDto("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            BillingAddress: new AddressDto("Sarah", "Connor", "sarah@example.com", "Main St", "US", "CA", "90001"),
            Payment: new PaymentDto("Sarah Connor", "4111111111111111", "12/30", "123", 1),
            Status: OrderStatus.Pending,
            OrderItems:
            [
                new OrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 1, 50m)
            ]);
    }
}