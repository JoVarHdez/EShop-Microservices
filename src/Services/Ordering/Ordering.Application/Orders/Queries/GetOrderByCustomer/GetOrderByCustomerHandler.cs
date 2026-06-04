using Microsoft.EntityFrameworkCore;
using Ordering.Application.Data;
using Ordering.Application.DTOs;
using Ordering.Application.Extensions;
using Ordering.Core.ValueObjects;

namespace Ordering.Application.Orders.Queries.GetOrderByCustomer
{
    public record GetOrderByCustomerResult(IEnumerable<OrderDto> Orders);

    public class GetOrderByCustomerHandler(IApplicationDbContext dbContext)
    {
        public async Task<GetOrderByCustomerResult> HandleAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var orders = await dbContext.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .Where(o => o.CustomerId == CustomerId.Of(customerId))
                .OrderBy(o => o.OrderName.Value)
                .ToListAsync(cancellationToken);

            return new GetOrderByCustomerResult(orders.ToOrderDtoList());
        }
    }
}
