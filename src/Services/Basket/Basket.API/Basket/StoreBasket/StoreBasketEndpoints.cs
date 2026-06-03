using Basket.API.Models;
using Mapster;
using Wolverine;

namespace Basket.API.Basket.StoreBasket
{
    public record StoreBasketRequest(ShoppingCart Cart);
    public record StoreBasketResponse(string UserName);

    public static class StoreBasketEndpoints
    {
        public static RouteGroupBuilder MapStoreBasketEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/", async (StoreBasketRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<StoreBasketCommand>();

                var result = await bus.InvokeAsync<StoreBasketResult>(command);

                var response = result.Adapt<StoreBasketResponse>();

                return Results.Created($"/basket/{response.UserName}", response);
            })
                .WithName("StoreBasket")
                .Produces<StoreBasketResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Stores a shopping basket for a user.")
                .WithDescription("Stores a shopping basket for a user.");

            return group;
        }
    }
}
