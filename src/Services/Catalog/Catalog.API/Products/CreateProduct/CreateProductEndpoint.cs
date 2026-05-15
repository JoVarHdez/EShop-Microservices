using Mapster;
using Wolverine;

namespace Catalog.API.Products.CreateProduct
{
    public record CreateProductRequest(
        string Name,
        string Description,
        decimal Price,
        string ImageUrl,
        List<string> Categories
    );

    public record CreateProductResponse(Guid Id);

    public static class CreateProductEndpoint
    {
        public static RouteGroupBuilder MapCreateProductEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/", async (CreateProductRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<CreateProductCommand>();

                var result = await bus.InvokeAsync<CreateProductResult>(command);

                var response = result.Adapt<CreateProductResponse>();

                return Results.Created($"/products/{response.Id}", response);
            })
                .WithName("CreateProduct")
                .Produces<CreateProductResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Creates a new product")
                .WithDescription("Creates a new product with the specified details");

            return group;
        }
    }
}
