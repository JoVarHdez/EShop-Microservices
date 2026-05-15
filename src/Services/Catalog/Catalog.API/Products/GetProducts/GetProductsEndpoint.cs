using Catalog.API.Models;
using Marten;
using Marten.Pagination;

namespace Catalog.API.Products.GetProducts
{
    public record GetProductsRequest(int? PageNumber = 1, int? PageSize = 10);
    public record GetProductsResponse(IEnumerable<Product> Products);

    public static class GetProductsEndpoint
    {
        public static RouteGroupBuilder MapGetProductsEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/", async ([AsParameters] GetProductsRequest request, IQuerySession session, CancellationToken ct) =>
            {
                var products = await session.Query<Product>()
                    .ToPagedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, ct);
                return TypedResults.Ok(new GetProductsResponse(products));
            })
                .WithName("GetProducts")
                .Produces<GetProductsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a list of products")
                .WithDescription("Gets a list of all products in the catalog");

            return group;
        }
    }
}
