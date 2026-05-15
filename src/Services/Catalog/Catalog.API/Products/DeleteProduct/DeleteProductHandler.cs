using Catalog.API.Models;
using FluentValidation;
using Marten;

namespace Catalog.API.Products.DeleteProduct
{
    public record DeleteProductCommand(Guid ProductId);

    public abstract record DeleteProductCommandResult;
    public record DeleteProductResult(bool IsSuccess) : DeleteProductCommandResult;
    public record DeleteProductNotFound : DeleteProductCommandResult;

    public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
    {
        public DeleteProductCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        }
    }

    public class DeleteProductCommandHandler(IDocumentSession session)
    {
        public async Task<DeleteProductCommandResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await session.LoadAsync<Product>(request.ProductId, cancellationToken);
            if (product is null)
                return new DeleteProductNotFound();

            session.Delete<Product>(request.ProductId);
            await session.SaveChangesAsync(cancellationToken);

            return new DeleteProductResult(true);
        }
    }
}
