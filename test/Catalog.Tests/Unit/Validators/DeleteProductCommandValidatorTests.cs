using Catalog.API.Products.DeleteProduct;

namespace Catalog.Tests.Unit.Validators;

public class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithEmptyProductId_ReturnsProductIdError()
    {
        var command = new DeleteProductCommand(Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task ValidateAsync_WithValidProductId_ReturnsSuccess()
    {
        var command = new DeleteProductCommand(Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
