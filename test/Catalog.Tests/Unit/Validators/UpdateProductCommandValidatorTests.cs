using Catalog.API.Products.UpdateProduct;

namespace Catalog.Tests.Unit.Validators;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        var command = new UpdateProductCommand(
            Id: Guid.NewGuid(),
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Updated description",
            ImageUrl: "https://example.com/product.png",
            Price: 1000m);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyId_ReturnsIdError()
    {
        var command = new UpdateProductCommand(
            Id: Guid.Empty,
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Updated description",
            ImageUrl: "https://example.com/product.png",
            Price: 1000m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task ValidateAsync_WithDescriptionOver250Chars_ReturnsDescriptionError()
    {
        var command = new UpdateProductCommand(
            Id: Guid.NewGuid(),
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: new string('a', 251),
            ImageUrl: "https://example.com/product.png",
            Price: 1000m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}
