using Refit;
using Shopping.Web.Razor.Models.Catalog;

namespace Shopping.Web.Razor.Services
{
    public interface ICatalogService
    {
        [Get("/catalog-service/products?pageNumber={pageNumber}&pageSize={pageSize}")]
        Task<GetProductsResponse> GetProductsAsync(int? pageNumber = 1, int? pageSize = 10);

        [Get("/catalog-service/products/{id}")]
        Task<GetProductByIdResponse> GetProductAsync(Guid id);

        [Get("/catalog-service/products/category/{category}")]
        Task<GetProductsByCategoryResponse> GetProductsByCategoryAsync(string category);
    }
}
