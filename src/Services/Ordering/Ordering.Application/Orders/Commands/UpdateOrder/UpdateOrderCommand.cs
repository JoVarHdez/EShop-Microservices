using BuildingBlocks.CQRS;
using FluentValidation;
using Ordering.Application.DTOs;

namespace Ordering.Application.Orders.Commands.UpdateOrder
{
    public record UpdateOrderCommand(OrderDto Order) : ICommand<UpdateOrderResult>;

    public record UpdateOrderResult(bool IsSuccess);

    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        public UpdateOrderCommandValidator()
        {
            RuleFor(x => x.Order.Id).NotEmpty().WithMessage("Order ID is required.");
            RuleFor(x => x.Order.CustomerId).NotNull().WithMessage("Customer ID is required.");
            RuleFor(x => x.Order.OrderName).NotEmpty().WithMessage("Order name is required.");
        }
    }
}
