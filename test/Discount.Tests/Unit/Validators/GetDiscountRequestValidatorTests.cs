using Discount.Grpc;
using Discount.Grpc.Validators;

namespace Discount.Tests.Unit.Validators;

public class GetDiscountRequestValidatorTests
{
    private readonly GetDiscountRequestValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidProductName_ReturnsSuccess()
    {
        var request = new GetDiscountRequest { ProductName = "IPhone X" };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyProductName_ReturnsError()
    {
        var request = new GetDiscountRequest { ProductName = string.Empty };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "ProductName");
    }
}
