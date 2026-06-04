using Ordering.Application.Orders.Commands.DeleteOrder;
using Wolverine;

namespace Ordering.API.Endpoints
{
    public record DeleteOrderResponse(bool IsSuccess);
    public static class DeleteOrderEndpoint
    {
        public static RouteGroupBuilder MapDeleteOrder(this RouteGroupBuilder group)
        {
            group.MapDelete("/{id}", async (Guid id, IMessageBus bus) =>
            {
                var result = await bus.InvokeAsync<DeleteOrderCommandResult>(new DeleteOrderCommand(id));

                return result switch
                {
                    DeleteOrderResult r => Results.Ok(new DeleteOrderResponse(r.IsSuccess)),
                    DeleteOrderNotFound => Results.NotFound(),
                    _ => Results.StatusCode(500)
                };
            })
                .WithName("DeleteOrder")
                .Produces<DeleteOrderResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Deletes an order.")
                .WithDescription("Deletes an order with the specified ID.");

            return group;
        }
    }
}
