using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Razor.Models.Ordering;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Pages
{
    public class OrderListModel(IOrderingService orderingService, IDevUserContextProvider devUserContextProvider, ILogger<OrderListModel> logger) : PageModel
    {
        public IEnumerable<OrderModel> Orders { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var customerId = devUserContextProvider.GetCurrent().CustomerId;

            var response = await orderingService.GetOrdersByCustomerAsync(customerId);
            Orders = response.Orders;

            return Page();
        }       
    }
}