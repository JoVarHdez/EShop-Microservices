using Mapster;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Queries.GetOrderByName;

namespace Ordering.API.Endpoints
{
    public record GetOrdersByNameResponse(IEnumerable<OrderDto> Orders);
    public static class GetOrdersByNameEndpoint
    {
        public static RouteGroupBuilder MapGetOrdersByName(this RouteGroupBuilder group)
        {
            group.MapGet("/{orderName}", async (string orderName, GetOrdersByNameHandler handler) =>
            {
                var result = await handler.HandleAsync(orderName);

                var response = result.Adapt<GetOrdersByNameResponse>();
                
                return Results.Ok(response);
            })
                .WithName("GetOrdersByName")
                .Produces<GetOrdersByNameResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets orders by name.")
                .WithDescription("Retrieves a list of orders that match the specified name.");

            return group;
        }
    }
}
