using Discount.Grpc;
using Discount.Grpc.Models;
using Discount.Grpc.Repository;
using Discount.Grpc.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Discount.Tests.Support;

namespace Discount.Tests.Unit.Services;

public class DiscountServiceTests
{
    [Fact]
    public async Task GetDiscount_WithValidProductId_UsesProductIdLookup()
    {
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.GetDiscountByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Coupon
            {
                Id = 10,
                ProductId = productId,
                ProductName = "IPhone X",
                Description = "Discount",
                Amount = 25
            });

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var response = await service.GetDiscount(new GetDiscountRequest { ProductId = productId.ToString(), ProductName = "fallback" }, CreateContext());

        Assert.Equal(10, response.Id);
        Assert.Equal(25, response.Amount);
        repository.Verify(x => x.GetDiscountByProductIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.GetDiscountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDiscount_WithInvalidProductId_FallsBackToProductNameLookup()
    {
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.GetDiscountAsync("IPhone X", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Coupon
            {
                Id = 22,
                ProductId = Guid.NewGuid(),
                ProductName = "IPhone X",
                Description = "Discount",
                Amount = 40
            });

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var response = await service.GetDiscount(new GetDiscountRequest { ProductId = "not-a-guid", ProductName = "IPhone X" }, CreateContext());

        Assert.Equal("IPhone X", response.ProductName);
        Assert.Equal(40, response.Amount);
        repository.Verify(x => x.GetDiscountByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GetDiscountAsync("IPhone X", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDiscount_WhenNotFound_ReturnsZeroAmountFallback()
    {
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.GetDiscountAsync("Unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var response = await service.GetDiscount(new GetDiscountRequest { ProductName = "Unknown" }, CreateContext());

        Assert.Equal("Unknown", response.ProductName);
        Assert.Equal(0, response.Amount);
        Assert.Equal("No discount available", response.Description);
    }

    [Fact]
    public async Task CreateDiscount_WhenRepositoryThrowsInvalidOperation_ThrowsAlreadyExistsRpcException()
    {
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.CreateDiscountAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate"));

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var exception = await Assert.ThrowsAsync<RpcException>(() => service.CreateDiscount(new CreateDiscountRequest
        {
            Coupon = new CouponModel { ProductName = "IPhone X", Description = "D", Amount = 10 }
        }, CreateContext()));

        Assert.Equal(StatusCode.AlreadyExists, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateDiscount_WhenCouponMissing_ThrowsNotFoundRpcException()
    {
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.UpdateDiscountAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var exception = await Assert.ThrowsAsync<RpcException>(() => service.UpdateDiscount(new UpdateDiscountRequest
        {
            Coupon = new CouponModel { ProductName = "Missing", Description = "D", Amount = 1 }
        }, CreateContext()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task DeleteDiscount_WhenCouponMissing_ThrowsNotFoundRpcException()
    {
        var repository = new Mock<IDiscountRepository>();
        repository.Setup(x => x.DeleteDiscountAsync("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new DiscountService(repository.Object, Mock.Of<ILogger<DiscountService>>());

        var exception = await Assert.ThrowsAsync<RpcException>(() => service.DeleteDiscount(new DeleteDiscountRequest { ProductName = "Missing" }, CreateContext()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    private static ServerCallContext CreateContext()
    {
        return new FakeServerCallContext();
    }
}
