using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Marten;
using Moq;

namespace Catalog.Tests.Unit.Handlers;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_StoresProductAndReturnsId()
    {
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.Store(It.IsAny<Product[]>()))
            .Callback<Product[]>(products => products[0].Id = Guid.NewGuid());
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new CreateProductCommandHandler(session.Object);
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone"],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        session.Verify(x => x.Store(It.IsAny<Product[]>()), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_MapsAllPropertiesFromCommand()
    {
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Product? captured = null;
        session.Setup(x => x.Store(It.IsAny<Product[]>()))
            .Callback<Product[]>(docs => captured = docs[0]);

        var handler = new CreateProductCommandHandler(session.Object);
        var command = new CreateProductCommand(
            Name: "IPhone X",
            Categories: ["Smart Phone", "Electronics"],
            Description: "Flagship smartphone",
            ImageUrl: "https://example.com/product.png",
            Price: 950m);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(command.Name, captured!.Name);
        Assert.Equal(command.Categories, captured.Categories);
        Assert.Equal(command.Description, captured.Description);
        Assert.Equal(command.ImageUrl, captured.ImageUrl);
        Assert.Equal(command.Price, captured.Price);
    }
}
