using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Ordering;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class OrderListModel(IOrderingService orderingService, ILogger<OrderListModel> logger) : PageModel
    {
        public IEnumerable<OrderModel> Orders { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            // Assume the user is authenticated and we can get the user id and name from the authentication context
            var customerId = new Guid("00000000-0000-0000-0000-000000000001");

            var response = await orderingService.GetOrdersByCustomerAsync(customerId);
            Orders = response.Orders;

            return Page();
        }       
    }
}