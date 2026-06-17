using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shopping.Web.Razor.Tests.Support;

public sealed class ShoppingWebAppFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IDictionary<string, string?>? _configurationOverrides;

    public ShoppingWebAppFactory(
        Action<IServiceCollection>? configureServices = null,
        IDictionary<string, string?>? configurationOverrides = null)
    {
        _configureServices = configureServices;
        _configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_configurationOverrides is not null)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_configurationOverrides);
            });
        }

        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }
    }
}
