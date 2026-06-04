using BuildingBlocks.Pagination;
using Mapster;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Queries.GetOrders;

namespace Ordering.API.Endpoints
{
    public record GetOrdersResponse(PaginatedResult<OrderDto> Orders);
    public static class GetOrdersEndpoint
    {
        public static RouteGroupBuilder MapGetOrders(this RouteGroupBuilder group)
        {
            group.MapGet("", async ([AsParameters] PaginationRequest request, GetOrdersHandler handler) =>
            {
                var result = await handler.HandleAsync(request);

                var response = result.Adapt<GetOrdersResponse>();

                return Results.Ok(response);
            })
                .WithName("GetOrders")
                .Produces<GetOrdersResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets all orders.")
                .WithDescription("Retrieves a list of all orders.");

            return group;
        }
    }
}
