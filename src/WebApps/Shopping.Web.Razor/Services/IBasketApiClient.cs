using Refit;
using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public interface IBasketApiClient
{
    [Get("/basket-service/basket/{userName}")]
    Task<GetBasketResponse> GetBasketAsync(string userName);

    [Post("/basket-service/basket")]
    Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request);

    [Delete("/basket-service/basket/{userName}")]
    Task<DeleteBasketResponse> DeleteBasketAsync(string userName);

    [Post("/basket-service/basket/checkout")]
    Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request);
}
