using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.DeleteProduct;
using Catalog.API.Products.GetProductByCategory;
using Catalog.API.Products.GetProductById;
using Catalog.API.Products.GetProducts;
using Catalog.API.Products.UpdateProduct;

namespace Catalog.API.Products
{
    public static class ProductsEndpoints
    {
        public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/products");

            group.MapCreateProductEndpoint();
            group.MapGetProductsEndpoint();
            group.MapGetProductByIdEndpoint();
            group.MapGetProductByCategoryEndpoint();
            group.MapUpdateProductEndpoint();
            group.MapDeleteProductEndpoint();

            return app;
        }
    }
}
