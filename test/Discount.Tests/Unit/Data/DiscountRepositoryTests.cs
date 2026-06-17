using Discount.Grpc.Data;
using Discount.Grpc.Models;
using Discount.Grpc.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Discount.Tests.Unit.Data;

public class DiscountRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = default!;
    private DiscountContext _context = default!;
    private DiscountRepository _repository = default!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DiscountContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DiscountContext(options);
        await _context.Database.EnsureCreatedAsync();
        _repository = new DiscountRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetDiscountAsync_WithSeededProduct_ReturnsCoupon()
    {
        var coupon = await _repository.GetDiscountAsync("IPhone X");

        Assert.NotNull(coupon);
        Assert.Equal("IPhone X", coupon.ProductName);
        Assert.Equal(150, coupon.Amount);
    }

    [Fact]
    public async Task GetDiscountByProductIdAsync_WithSeededProduct_ReturnsCoupon()
    {
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var coupon = await _repository.GetDiscountByProductIdAsync(productId);

        Assert.NotNull(coupon);
        Assert.Equal("IPhone X", coupon.ProductName);
    }

    [Fact]
    public async Task CreateDiscountAsync_WithDuplicateProductName_ThrowsInvalidOperationException()
    {
        var duplicate = new Coupon
        {
            ProductId = Guid.NewGuid(),
            ProductName = "IPhone X",
            Description = "Duplicate",
            Amount = 12
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateDiscountAsync(duplicate));
    }

    [Fact]
    public async Task CreateDiscountAsync_WithNewCoupon_PersistsCoupon()
    {
        var coupon = new Coupon
        {
            ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ProductName = "Pixel",
            Description = "Pixel Discount",
            Amount = 30
        };

        var created = await _repository.CreateDiscountAsync(coupon);
        var persisted = await _context.Coupons.FirstOrDefaultAsync(x => x.ProductName == "Pixel");

        Assert.Equal("Pixel", created.ProductName);
        Assert.NotNull(persisted);
        Assert.Equal(30, persisted.Amount);
    }

    [Fact]
    public async Task UpdateDiscountAsync_WithExistingCoupon_UpdatesMutableFields()
    {
        var update = new Coupon
        {
            ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ProductName = "IPhone X",
            Description = "Updated",
            Amount = 5
        };

        var updated = await _repository.UpdateDiscountAsync(update);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal(5, updated.Amount);
    }

    [Fact]
    public async Task UpdateDiscountAsync_WithMissingCoupon_ReturnsNull()
    {
        var update = new Coupon
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Missing",
            Description = "N/A",
            Amount = 1
        };

        var updated = await _repository.UpdateDiscountAsync(update);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteDiscountAsync_WithExistingCoupon_ReturnsTrueAndRemoves()
    {
        var deleted = await _repository.DeleteDiscountAsync("Samsung 10");
        var existing = await _repository.GetDiscountAsync("Samsung 10");

        Assert.True(deleted);
        Assert.Null(existing);
    }

    [Fact]
    public async Task DeleteDiscountAsync_WithMissingCoupon_ReturnsFalse()
    {
        var deleted = await _repository.DeleteDiscountAsync("Missing");

        Assert.False(deleted);
    }
}
