using Mapster;
using Wolverine;

namespace Catalog.API.Products.DeleteProduct
{
    public record DeleteProductResponse(bool IsSuccess);

    public static class DeleteProductEndpoint
    {
        public static RouteGroupBuilder MapDeleteProductEndpoint(this RouteGroupBuilder group)
        {
            group.MapDelete("/{id}", async (Guid id, IMessageBus bus) =>
            {
                var result = await bus.InvokeAsync<DeleteProductCommandResult>(new DeleteProductCommand(id));

                return result switch
                {
                    DeleteProductResult r   => Results.Ok(r.Adapt<DeleteProductResponse>()),
                    DeleteProductNotFound   => Results.NotFound(),
                    _                       => Results.Problem("Unexpected result", statusCode: 500)
                };
            })
                .WithName("DeleteProduct")
                .Produces<DeleteProductResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Deletes a product")
                .WithDescription("Deletes the product with the specified ID from the catalog");

            return group;
        }
    }
}
