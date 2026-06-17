using Basket.API.Basket.DeleteBasket;

namespace Basket.Tests.Unit.Validators;

public class DeleteBasketCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        var validator = new DeleteBasketCommandValidator();

        var result = await validator.ValidateAsync(new DeleteBasketCommand("sarah"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyUserName_ReturnsError()
    {
        var validator = new DeleteBasketCommandValidator();

        var result = await validator.ValidateAsync(new DeleteBasketCommand(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UserName");
    }
}
