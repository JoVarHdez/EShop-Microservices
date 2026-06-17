using Discount.Tests.Support;

namespace Discount.Tests.Integration.Endpoints;

public class DiscountGrpcEndpointsTests
{
    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        await using var host = await DiscountGrpcTestHost.StartAsync();

        var response = await host.Client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetDiscountRoute_IsMapped()
    {
        await using var host = await DiscountGrpcTestHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/discount.DiscountProtoService/GetDiscount");
        request.Headers.TryAddWithoutValidation("te", "trailers");
        request.Version = new Version(2, 0);
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        request.Content = new ByteArrayContent(Array.Empty<byte>());
        request.Content.Headers.TryAddWithoutValidation("content-type", "application/grpc");

        var response = await host.Client.SendAsync(request);

        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
