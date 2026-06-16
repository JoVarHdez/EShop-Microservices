using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class CheckoutModel(IBasketService basketService, IDevUserContextProvider devUserContextProvider, ILogger<CheckoutModel> logger)
        : PageModel
    {
        [BindProperty]
        public BasketCheckoutModel Order { get; set; } = default!;

        public ShoppingCartModel Cart { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            Cart = await basketService.LoadUserBasketAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCheckOutAsync()
        {
            logger.LogInformation("Checkout page is called.");
            Cart = await basketService.LoadUserBasketAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var currentUser = devUserContextProvider.GetCurrent();
            Order.CustomerId = currentUser.CustomerId;
            Order.UserName = currentUser.UserName;
            Order.TotalPrice = Cart.TotalPrice;

            await basketService.CheckoutBasketAsync(new CheckoutBasketRequest(Order));
            
            return RedirectToPage("Confirmation", "OrderSubmitted");
        }       
    }
}