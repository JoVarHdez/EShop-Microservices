using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class CartModel(IBasketService basketService, ILogger<CartModel> logger) : PageModel
    {
        public ShoppingCartModel Cart { get; set; } = new ShoppingCartModel();        

        public async Task<IActionResult> OnGetAsync()
        {
            Cart = await basketService.LoadUserBasketAsync();            
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveToCartAsync(Guid productId)
        {
            logger.LogInformation("Removing product {ProductId} from the cart.", productId);
            Cart = await basketService.LoadUserBasketAsync();
            
            Cart.Items.RemoveAll(item => item.ProductId == productId);

            await basketService.StoreBasketAsync(new StoreBasketRequest(Cart));

            return RedirectToPage();
        }
    }
}