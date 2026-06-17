using Basket.API.Basket.StoreBasket;
using Basket.API.Models;

namespace Basket.Tests.Unit.Validators;

public class StoreBasketCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        var validator = new StoreBasketCommandValidator();
        var command = new StoreBasketCommand(new ShoppingCart("sarah"));

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithNullCart_ReturnsError()
    {
        var validator = new StoreBasketCommandValidator();
        var command = new StoreBasketCommand(null!);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Cart");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyUserName_ReturnsError()
    {
        var validator = new StoreBasketCommandValidator();
        var command = new StoreBasketCommand(new ShoppingCart { UserName = string.Empty });

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Cart.UserName");
    }
}
