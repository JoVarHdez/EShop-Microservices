using Basket.API.Basket.CheckoutBasket;
using Basket.API.Basket.DeleteBasket;
using Basket.API.Basket.GetBasket;
using Basket.API.Basket.StoreBasket;

namespace Basket.API.Basket
{
    public static class BasketEndpoints
    {
        public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/basket");

            group.MapGetBasketEndpoint();
            group.MapStoreBasketEndpoint();
            group.MapDeleteBasketEndpoint();
            group.MapCheckoutBasketEndpoint();

            return app;
        }
    }
}
