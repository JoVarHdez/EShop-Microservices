using Mapster;
using Wolverine;

namespace Basket.API.Basket.DeleteBasket
{
    public record DeleteBasketResponse(bool Success);

    public static class DeleteBasketEndpoints
    {
        public static RouteGroupBuilder MapDeleteBasketEndpoint(this RouteGroupBuilder group)
        {
            group.MapDelete("/{userName}", async (string userName, IMessageBus bus) =>
            {
                var result = await bus.InvokeAsync<DeleteBasketResult>(new DeleteBasketCommand(userName));

                var response = result.Adapt<DeleteBasketResponse>();

                return Results.Ok(response);
            })
                .WithName("DeleteBasket")
                .Produces<DeleteBasketResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Deletes a shopping basket for a user.")
                .WithDescription("Deletes a shopping basket for a user.");

            return group;
        }
    }
}
