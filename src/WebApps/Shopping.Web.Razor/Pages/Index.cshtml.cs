using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models.Catalog;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class IndexModel(ICatalogService catalogService, IBasketService basketService, ILogger<IndexModel> logger) : PageModel
    {
        public IEnumerable<ProductModel> ProductList { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            logger.LogInformation("Getting products for the home page.");
            var result = await catalogService.GetProductsAsync();
            ProductList = result.Products;
            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(Guid productId)
        {
            logger.LogInformation("Adding product {ProductId} to the cart.", productId);
            var productResult = await catalogService.GetProductAsync(productId);
            var product = productResult.Product;

            if (product == null)
            {
                logger.LogWarning("Product with ID {ProductId} not found.", productId);
                return NotFound();
            }

            var basket = await basketService.LoadUserBasketAsync();

            basket.Items.Add(new ShoppingCartItemModel

            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = 1,
                Color = "Black",
            });

            await basketService.StoreBasketAsync(new StoreBasketRequest(basket));
            logger.LogInformation("Product {ProductId} added to the cart successfully.", productId);

            return RedirectToPage("Cart");
        }
    }
}
