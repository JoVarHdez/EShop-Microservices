using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models.Catalog;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class ProductDetailModel(ICatalogService catalogService, IBasketService basketService, ILogger<ProductDetailModel> logger)
        : PageModel
    {
        public ProductModel Product { get; set; } = default!;

        [BindProperty]
        public string Color { get; set; } = default!;

        [BindProperty]
        public int Quantity { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid productId)
        {
            var response = await catalogService.GetProductAsync(productId);
            Product = response.Product;

            if (Product == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(Guid productId)
        {
            logger.LogInformation("Adding product {ProductId} to the cart.", productId);
            var productResult = await catalogService.GetProductAsync(productId);

            var basket = await basketService.LoadUserBasket();

            basket.Items.Add(new ShoppingCartItemModel
            {
                ProductId = productId,
                ProductName = productResult.Product.Name,
                Price = productResult.Product.Price,
                Quantity = 1,
                Color = "Black"
            });

            await basketService.StoreBasketAsync(new StoreBasketRequest(basket));
            return RedirectToPage("Cart");
        }
    }
}