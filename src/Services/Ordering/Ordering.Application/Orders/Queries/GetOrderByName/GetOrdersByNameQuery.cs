using BuildingBlocks.CQRS;
using Ordering.Application.DTOs;

namespace Ordering.Application.Orders.Queries.GetOrderByName
{
    public record GetOrdersByNameQuery(string Name) : IQuery<GetOrdersByNameResult>;
    public record GetOrdersByNameResult(IEnumerable<OrderDto> Orders);
}
