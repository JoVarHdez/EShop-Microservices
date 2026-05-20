using Basket.API.Data;
using Basket.API.Models;
using BuildingBlocks.CQRS;
using Discount.Grpc;
using FluentValidation;

namespace Basket.API.Basket.StoreBasket
{
    public record StoreBasketCommand(ShoppingCart Cart) : ICommand<StoreBasketResult>;

    public record StoreBasketResult(string UserName);

    public class StoreBasketCommandValidator : AbstractValidator<StoreBasketCommand>
    {
        public StoreBasketCommandValidator()
        {
            RuleFor(x => x.Cart).NotNull().WithMessage("Shopping cart cannot be null.");
            RuleFor(x => x.Cart.UserName).NotEmpty().WithMessage("User name cannot be empty.");
        }
    }

    public class StoreBasketCommandHandler(IBasketRepository repository, DiscountProtoService.DiscountProtoServiceClient discountProtoService) 
        : ICommandHandler<StoreBasketCommand, StoreBasketResult>
    {
        public async Task<StoreBasketResult> Handle(StoreBasketCommand request, CancellationToken cancellationToken)
        {
            await DeductDiscount(request.Cart, cancellationToken);

            await repository.StoreBasketAsync(request.Cart, cancellationToken);

            return new StoreBasketResult(request.Cart.UserName);
        }

        private async Task DeductDiscount(ShoppingCart cart, CancellationToken cancellationToken)
        {
            // Communicate with Discount gRPC service to get the discount amount for each product in the shopping cart and deduct it from the product price
            foreach (var product in cart.Items)
            {
                var discount = await discountProtoService.GetDiscountAsync(new GetDiscountRequest { ProductName = product.ProductName }, cancellationToken: cancellationToken);
                product.Price -= discount.Amount;
            }
        }
    }
}
