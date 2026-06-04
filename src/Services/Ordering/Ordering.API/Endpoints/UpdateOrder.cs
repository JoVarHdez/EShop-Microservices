using Mapster;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.UpdateOrder;
using Wolverine;

namespace Ordering.API.Endpoints
{
    public record UpdateOrderRequest(OrderDto Order);
    public record UpdateOrderResponse(bool IsSuccess);
    public static class UpdateOrderEndpoint
    {
        public static RouteGroupBuilder MapUpdateOrder(this RouteGroupBuilder group)
        {
            group.MapPut("", async (UpdateOrderRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<UpdateOrderCommand>();
                var result = await bus.InvokeAsync<UpdateOrderCommandResult>(command);

                return result switch
                {
                    UpdateOrderResult r => Results.Ok(new UpdateOrderResponse(r.IsSuccess)),
                    UpdateOrderNotFound => Results.NotFound(),
                    _ => Results.StatusCode(500)
                };
            })
                .WithName("UpdateOrder")
                .Produces<UpdateOrderResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates an existing order.")
                .WithDescription("Updates an existing order with the specified details.");

            return group;
        }
    }
}
