using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public class BasketService(IBasketApiClient basketApiClient) : IBasketService
{
    private const string DefaultUserName = "swn";

    public async Task<ShoppingCartModel> LoadUserBasketAsync()
    {
        try
        {
            var response = await basketApiClient.GetBasketAsync(DefaultUserName);
            return response.Cart;
        }
        catch (Exception)
        {
            return new ShoppingCartModel
            {
                UserName = DefaultUserName,
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
