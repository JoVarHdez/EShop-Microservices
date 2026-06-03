using Discount.Grpc.Data;
using Discount.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Repository;

public class DiscountRepository(DiscountContext dbContext) : IDiscountRepository
{
    public async Task<Coupon?> GetDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == productName, cancellationToken);
    }

    public async Task<Coupon> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == coupon.ProductName, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException($"A coupon for '{coupon.ProductName}' already exists.");
        }

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return coupon;
    }

    public async Task<Coupon?> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == coupon.ProductName, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        existing.Description = coupon.Description;
        existing.Amount = coupon.Amount;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        var coupon = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == productName, cancellationToken);

        if (coupon is null)
        {
            return false;
        }

        dbContext.Coupons.Remove(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}