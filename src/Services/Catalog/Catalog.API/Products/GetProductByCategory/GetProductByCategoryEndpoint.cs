using Carter;
using Catalog.API.Models;
using Mapster;
using MediatR;

namespace Catalog.API.Products.GetProductByCategory
{
    public record GetProductsByCategoryResponse(IEnumerable<Product> Products);
    public class GetProductByCategoryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products/category/{categoryId}", async (string categoryId, ISender sender) =>
            {
                var result = await sender.Send(new GetProductByCategoryQuery(categoryId));
                var response = result.Adapt<GetProductsByCategoryResponse>();
                return Results.Ok(response);
            })
                .WithName("GetProductByCategory")
                .Produces<GetProductsByCategoryResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets products by category")
                .WithDescription("Retrieves a list of products that belong to the specified category.");
        }
    }
}
