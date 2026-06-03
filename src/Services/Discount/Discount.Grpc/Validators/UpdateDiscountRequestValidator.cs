using FluentValidation;

namespace Discount.Grpc.Validators;

public class UpdateDiscountRequestValidator : AbstractValidator<UpdateDiscountRequest>
{
    public UpdateDiscountRequestValidator()
    {
        RuleFor(x => x.Coupon).NotNull().WithMessage("Coupon is required.");

        When(x => x.Coupon is not null, () =>
        {
            RuleFor(x => x.Coupon!.ProductName)
                .NotEmpty().WithMessage("ProductName is required.");
            RuleFor(x => x.Coupon!.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Amount must be 0 or greater.");
        });
    }
}