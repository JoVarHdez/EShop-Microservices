using Mapster;
using Wolverine;

namespace Catalog.API.Products.UpdateProduct
{
    public record UpdateProductRequest(Guid Id, string Name, List<string> Categories, string Description, string ImageUrl, decimal Price);
    public record UpdateProductResponse(bool IsSuccess);

    public static class UpdateProductEndpoint
    {
        public static RouteGroupBuilder MapUpdateProductEndpoint(this RouteGroupBuilder group)
        {
            group.MapPut("/{id}", async (Guid id, UpdateProductRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<UpdateProductCommand>();
                var result = await bus.InvokeAsync<UpdateProductCommandResult>(command);

                return result switch
                {
                    UpdateProductResult r   => Results.Ok(r.Adapt<UpdateProductResponse>()),
                    UpdateProductNotFound   => Results.NotFound(),
                    _                       => Results.Problem("Unexpected result", statusCode: 500)
                };
            })
                .WithName("UpdateProduct")
                .Produces<UpdateProductResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Updates an existing product")
                .WithDescription("Updates the details of an existing product with the specified ID");

            return group;
        }
    }
}
