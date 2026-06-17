using Basket.API.Basket.CheckoutBasket;
using Basket.API.DTOs;

namespace Basket.Tests.Unit.Validators;

public class CheckoutBasketCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        var validator = new CheckoutBasketCommandValidator();
        var command = new CheckoutBasketCommand(new BasketCheckoutDto
        {
            UserName = "sarah"
        });

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithNullCheckoutDto_ReturnsError()
    {
        var validator = new CheckoutBasketCommandValidator();
        var command = new CheckoutBasketCommand(null!);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "BasketCheckoutDto");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyUserName_ReturnsError()
    {
        var validator = new CheckoutBasketCommandValidator();
        var command = new CheckoutBasketCommand(new BasketCheckoutDto
        {
            UserName = string.Empty
        });

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "BasketCheckoutDto.UserName");
    }
}
