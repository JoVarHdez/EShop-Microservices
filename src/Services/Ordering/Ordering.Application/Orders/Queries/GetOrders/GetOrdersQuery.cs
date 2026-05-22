using BuildingBlocks.CQRS;
using BuildingBlocks.Pagination;
using Ordering.Application.DTOs;

namespace Ordering.Application.Orders.Queries.GetOrders
{
    public record GetOrdersQuery(PaginationRequest PaginationRequest) : IQuery<GetOrdersResult>;

    public record GetOrdersResult(PaginatedResult<OrderDto> Orders);
}
