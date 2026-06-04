using Microsoft.EntityFrameworkCore;
using Ordering.Application.Data;
using Ordering.Application.DTOs;
using Ordering.Application.Extensions;

namespace Ordering.Application.Orders.Queries.GetOrderByName
{
    public record GetOrdersByNameResult(IEnumerable<OrderDto> Orders);

    public class GetOrdersByNameHandler(IApplicationDbContext dbContext)
    {
        public async Task<GetOrdersByNameResult> HandleAsync(string name, CancellationToken cancellationToken = default)
        {
            var orders = await dbContext.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .Where(o => o.OrderName.Value.Contains(name))
                .OrderBy(o => o.OrderName.Value)
                .ToListAsync(cancellationToken);

            return new GetOrdersByNameResult(orders.ToOrderDtoList());
        }
    }
}
