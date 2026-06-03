using Basket.API.DTOs;
using Mapster;
using Wolverine;

namespace Basket.API.Basket.CheckoutBasket
{
    public record CheckoutBasketRequest(BasketCheckoutDto BasketCheckoutDto);
    public record CheckoutBasketResponse(bool IsSuccess);

    public static class CheckoutBasketEndpoints
    {
        public static RouteGroupBuilder MapCheckoutBasketEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/checkout", async (CheckoutBasketRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<CheckoutBasketCommand>();

                var result = await bus.InvokeAsync<CheckoutBasketResult>(command);

                if (!result.IsSuccess)
                    return Results.NotFound();

                var response = result.Adapt<CheckoutBasketResponse>();

                return Results.Ok(response);
            })
                .WithName("CheckoutBasket")
                .Produces<CheckoutBasketResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Checkout a basket")
                .WithDescription("Checkout a basket with the provided details");

            return group;
        }
    }
}
