using BuildingBlocks.CQRS;
using Ordering.Application.Data;
using Ordering.Application.Exceptions;
using Ordering.Core.ValueObjects;

namespace Ordering.Application.Orders.Commands.DeleteOrder
{
    public class DeleteOrderHandler(IApplicationDbContext dbContext) : ICommandHandler<DeleteOrderCommand, DeleteOrderResult>
    {
        public async Task<DeleteOrderResult> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Of(request.OrderId);
            var order = await dbContext.Orders.FindAsync([orderId], cancellationToken);

            if (order == null)
            {
                throw new OrderNotFoundException(request.OrderId);
            }

            dbContext.Orders.Remove(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DeleteOrderResult(true);
        }
    }
}
