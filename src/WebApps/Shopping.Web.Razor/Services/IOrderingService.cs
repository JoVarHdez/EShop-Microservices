using Refit;
using Shopping.Web.Razor.Models.Ordering;

namespace Shopping.Web.Razor.Services
{
    public interface IOrderingService
    {
        [Get("/ordering-service/orders?pageIndex={pageIndex}&pageSize={pageSize}")]
        Task<GetOrdersResponse> GetOrdersAsync(int? pageIndex = 1, int? pageSize = 10);

        [Get("/ordering-service/orders/{orderName}")]
        Task<GetOrdersByNameResponse> GetOrdersByNameAsync(string orderName);

        [Get("/ordering-service/orders/customer/{customerId}")]
        Task<GetOrdersByCustomerResponse> GetOrdersByCustomerAsync(Guid customerId);
    }
}
