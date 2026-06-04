using Mapster;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Queries.GetOrderByCustomer;

namespace Ordering.API.Endpoints
{
    public record GetOrdersByCustomerResponse(IEnumerable<OrderDto> Orders);
    public static class GetOrdersByCustomerEndpoint
    {
        public static RouteGroupBuilder MapGetOrdersByCustomer(this RouteGroupBuilder group)
        {
            group.MapGet("/customer/{customerId}", async (Guid customerId, GetOrderByCustomerHandler handler) =>
            {
                var result = await handler.HandleAsync(customerId);

                var response = result.Adapt<GetOrdersByCustomerResponse>();

                return Results.Ok(response);
            })
                .WithName("GetOrdersByCustomer")
                .Produces<GetOrdersByCustomerResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets orders by customer ID.")
                .WithDescription("Retrieves a list of orders associated with the specified customer ID.");

            return group;
        }
    }
}
