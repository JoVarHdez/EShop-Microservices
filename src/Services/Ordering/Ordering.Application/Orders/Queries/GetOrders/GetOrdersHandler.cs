using BuildingBlocks.Pagination;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Data;
using Ordering.Application.DTOs;
using Ordering.Application.Extensions;

namespace Ordering.Application.Orders.Queries.GetOrders
{
    public record GetOrdersResult(PaginatedResult<OrderDto> Orders);

    public class GetOrdersHandler(IApplicationDbContext dbContext)
    {
        public async Task<GetOrdersResult> HandleAsync(PaginationRequest paginationRequest, CancellationToken cancellationToken = default)
        {
            var pageIndex = paginationRequest.PageIndex;
            var pageSize = paginationRequest.PageSize;

            var totalCount = await dbContext.Orders.LongCountAsync(cancellationToken);

            var orders = await dbContext.Orders
                .Include(o => o.OrderItems)
                .OrderBy(o => o.OrderName.Value)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new GetOrdersResult(new PaginatedResult<OrderDto>(
                pageIndex,
                pageSize,
                totalCount,
                orders.ToOrderDtoList()));
        }
    }
}
