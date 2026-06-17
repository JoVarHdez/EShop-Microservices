using System.Net;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Integration.Provider;

[Collection(ShoppingProviderCollection.CollectionName)]
[Trait("Category", "RequiresDocker")]
public class ShoppingGatewaySmokeTests
{
    private const string RequireDockerEnvVar = "SHOPPING_REQUIRE_DOCKER_TESTS";
    private static readonly HttpClient Client = new() { BaseAddress = new Uri("http://localhost:6004") };

    [Fact]
    public async Task Gateway_CatalogProductsEndpoint_IsReachable()
    {
        if (!EnsureEnabledAndDocker())
        {
            return;
        }

        var response = await Client.GetAsync("/catalog-service/products?pageNumber=1&pageSize=5");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound or HttpStatusCode.BadGateway,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task Gateway_OrderingByCustomerEndpoint_IsReachable()
    {
        if (!EnsureEnabledAndDocker())
        {
            return;
        }

        var response = await Client.GetAsync($"/ordering-service/orders/customer/{TestData.CustomerId}");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound or HttpStatusCode.BadGateway,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task Gateway_BasketEndpoint_IsReachable()
    {
        if (!EnsureEnabledAndDocker())
        {
            return;
        }

        var response = await Client.GetAsync("/basket-service/basket/swn");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound or HttpStatusCode.BadGateway,
            $"Unexpected status: {response.StatusCode}");
    }

    private static bool EnsureEnabledAndDocker()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable(RequireDockerEnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!enabled)
        {
            return false;
        }

        DockerAvailability.EnsureDockerOrFail("Docker is required for provider-backed Shopping.Web.Razor tests.");
        return true;
    }
}
