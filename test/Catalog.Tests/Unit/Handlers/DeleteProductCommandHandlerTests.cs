using Catalog.API.Models;
using Catalog.API.Products.DeleteProduct;
using Marten;
using Moq;

namespace Catalog.Tests.Unit.Handlers;

public class DeleteProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingProduct_DeletesProductAndReturnsSuccess()
    {
        var productId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, Name = "P", Categories = ["A"], Description = "D", ImageUrl = "u", Price = 1m });
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new DeleteProductCommandHandler(session.Object);

        var result = await handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        var success = Assert.IsType<DeleteProductResult>(result);
        Assert.True(success.IsSuccess);
        session.Verify(x => x.Delete<Product>(productId), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ReturnsNotFoundResult()
    {
        var productId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new DeleteProductCommandHandler(session.Object);

        var result = await handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        Assert.IsType<DeleteProductNotFound>(result);
        session.Verify(x => x.Delete<Product>(It.IsAny<Guid>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
