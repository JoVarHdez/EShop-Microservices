using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.UpdateOrder;
using Ordering.Core.Enums;

namespace Ordering.Tests.Unit.Validators;

public class UpdateOrderCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new UpdateOrderCommandValidator();

        var result = validator.Validate(new UpdateOrderCommand(CreateValidOrderDto()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyOrderId_Fails()
    {
        var validator = new UpdateOrderCommandValidator();
        var command = new UpdateOrderCommand(CreateValidOrderDto() with { Id = Guid.Empty });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Order.Id");
    }

    [Fact]
    public void Validate_WithEmptyOrderName_Fails()
    {
        var validator = new UpdateOrderCommandValidator();
        var command = new UpdateOrderCommand(CreateValidOrderDto() with { OrderName = string.Empty });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Order.OrderName");
    }

    private static OrderDto CreateValidOrderDto()
    {
        return new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderName: "ORD-200",
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