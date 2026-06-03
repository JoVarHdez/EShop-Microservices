using Discount.Grpc.Data;
using Discount.Grpc.Models;
using Discount.Grpc.Repository;
using Grpc.Core;
using Mapster;

namespace Discount.Grpc.Services
{
    public class DiscountService(IDiscountRepository repository, ILogger<DiscountService> logger)
        : DiscountProtoService.DiscountProtoServiceBase
    {
        public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
        {
            var coupon = await repository.GetDiscountAsync(request.ProductName, context.CancellationToken)
                ?? new Coupon
            {
                ProductName = request.ProductName,
                Amount = 0,
                Description = "No discount available"
            };

            logger.LogInformation("Discount retrieved for ProductName: {ProductName}, Amount: {Amount}", coupon.ProductName, coupon.Amount);

            var couponModel = coupon.Adapt<CouponModel>();
            return couponModel;
        }

        public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
        {
            var coupon = request.Coupon.Adapt<Coupon>();

            try
            {
                await repository.CreateDiscountAsync(coupon, context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
            }

            logger.LogInformation("Discount created for ProductName: {ProductName}, Amount: {Amount}", coupon.ProductName, coupon.Amount);

            var couponModel = coupon.Adapt<CouponModel>();
            return couponModel;
        }

        public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
        {
            var coupon = request.Coupon.Adapt<Coupon>();
            var updated = await repository.UpdateDiscountAsync(coupon, context.CancellationToken);

            if (updated is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Discount for '{request.Coupon.ProductName}' not found."));
            }

            logger.LogInformation("Discount updated for ProductName: {ProductName}, Amount: {Amount}", updated.ProductName, updated.Amount);

            var couponModel = updated.Adapt<CouponModel>();
            return couponModel;
        }

        public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
        {
            var deleted = await repository.DeleteDiscountAsync(request.ProductName, context.CancellationToken);

            if (!deleted)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Discount for ProductName: {request.ProductName} not found"));
            }

            logger.LogInformation("Discount deleted for ProductName: {ProductName}", request.ProductName);

            return new DeleteDiscountResponse { Success = true };
        }
    }
}
