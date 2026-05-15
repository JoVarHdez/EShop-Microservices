using Catalog.API.Models;
using Marten;

namespace Catalog.API.Products.GetProductByCategory
{
    public record GetProductsByCategoryResponse(IEnumerable<Product> Products);

    public static class GetProductByCategoryEndpoint
    {
        public static RouteGroupBuilder MapGetProductByCategoryEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/category/{categoryId}", async (string categoryId, IQuerySession session, CancellationToken ct) =>
            {
                var products = await session.Query<Product>()
                    .Where(p => p.Categories.Contains(categoryId))
                    .ToListAsync(ct);
                return TypedResults.Ok(new GetProductsByCategoryResponse(products));
            })
                .WithName("GetProductByCategory")
                .Produces<GetProductsByCategoryResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets products by category")
                .WithDescription("Retrieves a list of products that belong to the specified category.");

            return group;
        }
    }
}
