using Catalog.API.Models;
using Marten;

namespace Catalog.API.Products.GetProductById
{
    public record GetProductByIdResponse(Product Product);

    public static class GetProductByIdEndpoint
    {
        public static RouteGroupBuilder MapGetProductByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("/{id}", async (Guid id, IQuerySession session, CancellationToken ct) =>
            {
                var product = await session.LoadAsync<Product>(id, ct);
                if (product is null) return Results.NotFound();
                return Results.Ok(new GetProductByIdResponse(product));
            })
                .WithName("GetProductById")
                .Produces<GetProductByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets a product by ID")
                .WithDescription("Retrieves the details of a product with the specified ID.");

            return group;
        }
    }
}
