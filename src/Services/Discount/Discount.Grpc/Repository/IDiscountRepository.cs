using Discount.Grpc.Models;

namespace Discount.Grpc.Repository;

public interface IDiscountRepository
{
    Task<Coupon?> GetDiscountAsync(string productName, CancellationToken cancellationToken = default);
    Task<Coupon> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<Coupon?> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default);
}