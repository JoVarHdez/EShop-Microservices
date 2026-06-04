using Mapster;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.CreateOrder;
using Wolverine;

namespace Ordering.API.Endpoints
{
    public record CreateOrderRequest(OrderDto Order);
    public record CreateOrderResponse(Guid Id);
    public static class CreateOrderEndpoint
    {
        public static RouteGroupBuilder MapCreateOrder(this RouteGroupBuilder group)
        {
            group.MapPost("", async (CreateOrderRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<CreateOrderCommand>();

                var result = await bus.InvokeAsync<CreateOrderResult>(command);

                var response = result.Adapt<CreateOrderResponse>();

                return Results.Created($"/orders/{response.Id}", response);
            })
                .WithName("CreateOrder")
                .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Creates a new order.")
                .WithDescription("Creates a new order with the specified details.");

            return group;
        }
    }
}
