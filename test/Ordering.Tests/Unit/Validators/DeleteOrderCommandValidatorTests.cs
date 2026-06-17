using Ordering.Application.Orders.Commands.DeleteOrder;

namespace Ordering.Tests.Unit.Validators;

public class DeleteOrderCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidOrderId_Passes()
    {
        var validator = new DeleteOrderCommandValidator();

        var result = validator.Validate(new DeleteOrderCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyOrderId_Fails()
    {
        var validator = new DeleteOrderCommandValidator();

        var result = validator.Validate(new DeleteOrderCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "OrderId");
    }
}