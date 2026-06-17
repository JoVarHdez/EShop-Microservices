using Discount.Grpc;
using Grpc.Core;

namespace Basket.Tests.Support.Fakes;

public sealed class FakeDiscountProtoServiceClient : DiscountProtoService.DiscountProtoServiceClient
{
    private readonly Func<GetDiscountRequest, CouponModel> _handler;

    public FakeDiscountProtoServiceClient(Func<GetDiscountRequest, CouponModel>? handler = null)
    {
        _handler = handler ?? (_ => new CouponModel { Amount = 0 });
    }

    public override AsyncUnaryCall<CouponModel> GetDiscountAsync(
        GetDiscountRequest request,
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        return BuildResponse(_handler(request));
    }

    public override AsyncUnaryCall<CouponModel> GetDiscountAsync(GetDiscountRequest request, CallOptions options)
    {
        return BuildResponse(_handler(request));
    }

    private static AsyncUnaryCall<CouponModel> BuildResponse(CouponModel model)
    {
        return new AsyncUnaryCall<CouponModel>(
            Task.FromResult(model),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
