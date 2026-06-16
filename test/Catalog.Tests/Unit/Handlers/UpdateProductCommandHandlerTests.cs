using Catalog.API.Models;
using Catalog.API.Products.UpdateProduct;
using Marten;
using Moq;

namespace Catalog.Tests.Unit.Handlers;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingProduct_UpdatesProductAndReturnsSuccess()
    {
        var productId = Guid.NewGuid();
        var existing = new Product
        {
            Id = productId,
            Name = "Old",
            Categories = ["Old"],
            Description = "Old",
            ImageUrl = "https://example.com/old.png",
            Price = 1m
        };

        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new UpdateProductCommandHandler(session.Object);
        var command = new UpdateProductCommand(
            Id: productId,
            Name: "New",
            Categories: ["Smart Phone"],
            Description: "New description",
            ImageUrl: "https://example.com/new.png",
            Price: 99m);

        var result = await handler.Handle(command, CancellationToken.None);

        var success = Assert.IsType<UpdateProductResult>(result);
        Assert.True(success.IsSuccess);
        session.Verify(x => x.Update(It.Is<Product>(p => p.Id == productId && p.Name == "New" && p.Price == 99m)), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ReturnsNotFoundResult()
    {
        var productId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new UpdateProductCommandHandler(session.Object);
        var command = new UpdateProductCommand(productId, "New", ["A"], "D", "https://example.com/new.png", 99m);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.IsType<UpdateProductNotFound>(result);
        session.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
