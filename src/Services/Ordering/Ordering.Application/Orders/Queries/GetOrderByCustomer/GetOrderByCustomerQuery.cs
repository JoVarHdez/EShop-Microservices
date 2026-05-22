using BuildingBlocks.CQRS;
using Ordering.Application.DTOs;

namespace Ordering.Application.Orders.Queries.GetOrderByCustomer
{
    public record GetOrderByCustomerQuery(Guid CustomerId) : IQuery<GetOrderByCustomerResult>;
    public record GetOrderByCustomerResult(IEnumerable<OrderDto> Orders);
}
