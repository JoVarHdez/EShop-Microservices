using Basket.API.Data;
using Basket.API.DTOs;
using BuildingBlocks.Messaging.Events;
using FluentValidation;
using Mapster;
using MassTransit;

namespace Basket.API.Basket.CheckoutBasket
{
    public record CheckoutBasketCommand(BasketCheckoutDto BasketCheckoutDto);
    public record CheckoutBasketResult(bool IsSuccess);

    public class CheckoutBasketCommandValidator : AbstractValidator<CheckoutBasketCommand>
    {
        public CheckoutBasketCommandValidator()
        {
            RuleFor(x => x.BasketCheckoutDto).NotNull().WithMessage("BasketCheckoutDto cannot be null.");
            RuleFor(x => x.BasketCheckoutDto.UserName)
                .NotEmpty().WithMessage("UserName cannot be empty.")
                .When(x => x.BasketCheckoutDto is not null);
        }
    }

    public class CheckoutBasketCommandHandler(IBasketRepository repository, IPublishEndpoint publishEndpoint)
    {
        public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
        {
            var basket = await repository.GetBasketAsync(command.BasketCheckoutDto.UserName, cancellationToken);
            if (basket == null)
            {
                return new CheckoutBasketResult(false);
            }

            var eventMessage = command.BasketCheckoutDto.Adapt<BasketCheckoutEvent>();
            eventMessage.TotalPrice = basket.TotalPrice;
            eventMessage.Items = [.. basket.Items.Select(item =>
                new BasketCheckoutLineItem(item.ProductId, item.ProductName, item.Quantity, item.Price))];

            await publishEndpoint.Publish(eventMessage, cancellationToken);

            await repository.DeleteBasketAsync(command.BasketCheckoutDto.UserName, cancellationToken);

            return new CheckoutBasketResult(true);
        }
    }
}
