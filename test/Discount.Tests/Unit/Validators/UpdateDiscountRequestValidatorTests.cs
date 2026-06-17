using Discount.Grpc;
using Discount.Grpc.Validators;

namespace Discount.Tests.Unit.Validators;

public class UpdateDiscountRequestValidatorTests
{
    private readonly UpdateDiscountRequestValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithAmountZero_ReturnsSuccess()
    {
        var request = new UpdateDiscountRequest
        {
            Coupon = new CouponModel
            {
                ProductName = "IPhone X",
                Amount = 0
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithNegativeAmount_ReturnsError()
    {
        var request = new UpdateDiscountRequest
        {
            Coupon = new CouponModel
            {
                ProductName = "IPhone X",
                Amount = -1
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Coupon.Amount");
    }
}
