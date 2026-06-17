using Discount.Grpc;
using Discount.Grpc.Validators;

namespace Discount.Tests.Unit.Validators;

public class CreateDiscountRequestValidatorTests
{
    private readonly CreateDiscountRequestValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateDiscountRequest
        {
            Coupon = new CouponModel
            {
                ProductName = "IPhone X",
                Description = "IPhone Discount",
                Amount = 50
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithNullCoupon_ReturnsError()
    {
        var request = new CreateDiscountRequest();

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Coupon");
    }

    [Fact]
    public async Task ValidateAsync_WithZeroAmount_ReturnsError()
    {
        var request = new CreateDiscountRequest
        {
            Coupon = new CouponModel
            {
                ProductName = "IPhone X",
                Amount = 0
            }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Coupon.Amount");
    }
}
