using FluentValidation;

namespace Discount.Grpc.Validators;

public class GetDiscountRequestValidator : AbstractValidator<GetDiscountRequest>
{
    public GetDiscountRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.");
    }
}