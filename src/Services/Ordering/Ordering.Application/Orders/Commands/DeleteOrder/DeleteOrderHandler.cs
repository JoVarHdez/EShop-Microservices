using Ordering.Application.Data;
using Ordering.Core.ValueObjects;

namespace Ordering.Application.Orders.Commands.DeleteOrder
{
    public class DeleteOrderHandler(IApplicationDbContext dbContext)
    {
        public async Task<DeleteOrderCommandResult> HandleAsync(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Of(request.OrderId);
            var order = await dbContext.Orders.FindAsync([orderId], cancellationToken);

            if (order is null)
                return new DeleteOrderNotFound();

            dbContext.Orders.Remove(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DeleteOrderResult(true);
        }
    }
}
