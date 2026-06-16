using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models;

namespace Shopping.Web.Razor.Services;

public class BasketService(IBasketApiClient basketApiClient, IDevUserContextProvider devUserContextProvider) : IBasketService
{
    private string CurrentUserName => devUserContextProvider.GetCurrent().UserName;

    public async Task<ShoppingCartModel> LoadUserBasketAsync()
    {
        try
        {
            var response = await basketApiClient.GetBasketAsync(CurrentUserName);
            return response.Cart;
        }
        catch (Exception)
        {
            return new ShoppingCartModel
            {
                UserName = CurrentUserName,
                Items = [],
            };
        }
    }

    public Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request)
        => basketApiClient.StoreBasketAsync(request);

    public Task<DeleteBasketResponse> DeleteBasketAsync(string userName)
        => basketApiClient.DeleteBasketAsync(userName);

    public Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request)
        => basketApiClient.CheckoutBasketAsync(request);
}
