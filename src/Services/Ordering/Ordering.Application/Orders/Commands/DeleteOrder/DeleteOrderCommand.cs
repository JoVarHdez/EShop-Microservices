using FluentValidation;

namespace Ordering.Application.Orders.Commands.DeleteOrder
{
    public record DeleteOrderCommand(Guid OrderId);
    public abstract record DeleteOrderCommandResult;
    public record DeleteOrderResult(bool IsSuccess) : DeleteOrderCommandResult;
    public sealed record DeleteOrderNotFound : DeleteOrderCommandResult;

    public class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
    {
        public DeleteOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        }
    }
}
