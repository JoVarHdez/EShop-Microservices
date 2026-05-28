using Refit;
using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services
{
    public interface IBasketService
    {
        [Get("/basket-service/basket/{userName}")]
        Task<GetBasketResponse> GetBasketAsync(string userName);

        [Post("/basket-service/basket")]
        Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request);

        [Delete("/basket-service/basket/{userName}")]
        Task<DeleteBasketResponse> DeleteBasketAsync(string userName);

        [Post("/basket-service/basket/checkout")]
        Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request);

        public async Task<ShoppingCartModel> LoadUserBasket()
        {
            // In a real application, you would get the user name from the authenticated user context.
            var userName = "swn";
            ShoppingCartModel basket;

            try
            {
                var getBasketResponse = await GetBasketAsync(userName);
                basket = getBasketResponse.Cart;
            }
            catch (Exception)
            {
                basket = new ShoppingCartModel
                {
                    UserName = userName,
                    Items = [],
                };
            }

            return basket;
        }
    }
}
