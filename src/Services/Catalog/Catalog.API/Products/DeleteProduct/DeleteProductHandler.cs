using BuildingBlocks.CQRS;
using Catalog.API.Models;
using Marten;

namespace Catalog.API.Products.DeleteProduct
{
    public record DeleteProductCommand(Guid ProductId) : ICommand<DeleteProductResult>;
    public record DeleteProductResult(bool IsSuccess);
    public class DeleteProductCommandHandler(IDocumentSession session) : ICommandHandler<DeleteProductCommand, DeleteProductResult>
    {
        public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            session.Delete<Product>(request.ProductId);
            await session.SaveChangesAsync(cancellationToken);

            return new DeleteProductResult(true);
        }
    }
}
