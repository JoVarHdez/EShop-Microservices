using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models.Catalog;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class ProductListModel(ICatalogService catalogService, IBasketService basketService,ILogger<ProductListModel> logger) : PageModel
    {
        public IEnumerable<string> CategoryList { get; set; } = [];
        public IEnumerable<ProductModel> ProductList { get; set; } = [];


        [BindProperty(SupportsGet = true)]
        public string SelectedCategory { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string categoryName)
        {
            var response = await catalogService.GetProductsAsync();

            CategoryList = response.Products.SelectMany(p => p.Category).Distinct();

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                ProductList = response.Products.Where(p => p.Category.Contains(categoryName));
                SelectedCategory = categoryName;
            }
            else
            {
                ProductList = response.Products;
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