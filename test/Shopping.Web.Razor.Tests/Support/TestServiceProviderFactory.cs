using Microsoft.Extensions.DependencyInjection;
using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor.Tests.Support;

public static class TestServiceProviderFactory
{
    public static ServiceProvider BuildWithApiClients(FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ApiSettings>().Configure(o => o.GatewayAddress = "https://example.test");
        services.AddSingleton<IDevUserContextProvider>(new StubDevUserContextProvider(TestData.DevUser()));
        services.AddApiClients(() => handler);

        return services.BuildServiceProvider();
    }

    private sealed class StubDevUserContextProvider(DevUserContext user) : IDevUserContextProvider
    {
        public DevUserContext GetCurrent() => user;
    }
}
