using Basket.API.Basket.DeleteBasket;
using Basket.API.Data;
using Moq;

namespace Basket.Tests.Unit.Handlers;

public class DeleteBasketCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_DeletesBasketAndReturnsSuccess()
    {
        var repository = new Mock<IBasketRepository>();
        repository.Setup(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new DeleteBasketCommandHandler(repository.Object);

        var result = await handler.Handle(new DeleteBasketCommand("sarah"), CancellationToken.None);

        Assert.True(result.Success);
        repository.Verify(x => x.DeleteBasketAsync("sarah", It.IsAny<CancellationToken>()), Times.Once);
    }
}
