using FluentValidation;
using Ordering.Application.DTOs;

namespace Ordering.Application.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(OrderDto Order);
    
    public record CreateOrderResult(Guid Id);

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.Order.OrderName).NotEmpty().WithMessage("Order name is required.");
            RuleFor(x => x.Order.CustomerId).NotNull().WithMessage("Customer ID is required.");
            RuleFor(x => x.Order.OrderItems).NotEmpty().WithMessage("At least one order item is required.");
        }
    }
}
