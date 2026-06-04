using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public interface IBasketService
{
    Task<ShoppingCartModel> LoadUserBasketAsync();
    Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request);
    Task<DeleteBasketResponse> DeleteBasketAsync(string userName);
    Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request);
}
