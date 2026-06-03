using Basket.API.Data;
using Basket.API.Models;

namespace Basket.API.Basket.GetBasket
{
    public record GetBasketResponse(ShoppingCart Cart);

    public static class GetBasketEndpoints
    {
        public static RouteGroupBuilder MapGetBasketEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/{userName}", async (string userName, IBasketRepository repository, CancellationToken ct) =>
            {
                var basket = await repository.GetBasketAsync(userName, ct);
                if (basket is null) return Results.NotFound();
                return Results.Ok(new GetBasketResponse(basket));
            })
                .WithName("GetBasket")
                .Produces<GetBasketResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Get the shopping cart for a specific user")
                .WithDescription("This endpoint retrieves the shopping cart associated with the specified user name.");

            return group;
        }
    }
}
