using MediatR;

namespace Catalog.API.Products.CreateProduct
{
    public record CreateProductCommand(string Name, List<string> Categories, string Description, string ImageUrl, decimal Price) : IRequest<CreateProductResult>;
    public record CreateProductResult(Guid Id);

    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
    {
        public Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Business logic to create a product would go here, such as validating the input, saving the product to a database, etc.
            throw new NotImplementedException();
        }
    }
}
