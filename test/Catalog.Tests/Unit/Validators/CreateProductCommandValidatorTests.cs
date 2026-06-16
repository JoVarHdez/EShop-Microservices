using Catalog.API.Products.CreateProduct;

namespace Catalog.Tests.Unit.Validators;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyName_ReturnsNameError()
    {
        var command = new CreateProductCommand(
            Name: string.Empty,
            Categories: ["Smart Phone"],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCategories_ReturnsCategoriesError()
    {
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: [],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Categories");
    }

    [Fact]
    public async Task ValidateAsync_WithDescriptionOver250Chars_ReturnsDescriptionError()
    {
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: new string('a', 251),
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidImageUrl_ReturnsImageUrlError()
    {
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Flagship smartphone",
            ImageUrl: "http://[]",
            Price: 950m);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "ImageUrl");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_WithInvalidPrice_ReturnsPriceError(decimal price)
    {
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: price);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }
}
