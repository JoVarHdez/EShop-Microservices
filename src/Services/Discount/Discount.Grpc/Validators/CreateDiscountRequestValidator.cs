using FluentValidation;

namespace Discount.Grpc.Validators;

public class CreateDiscountRequestValidator : AbstractValidator<CreateDiscountRequest>
{
    public CreateDiscountRequestValidator()
    {
        RuleFor(x => x.Coupon).NotNull().WithMessage("Coupon is required.");

        When(x => x.Coupon is not null, () =>
        {
            RuleFor(x => x.Coupon!.ProductName)
                .NotEmpty().WithMessage("ProductName is required.");
            RuleFor(x => x.Coupon!.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0.");
        });
    }
}