using Microsoft.Extensions.Options;
using Refit;
using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor;

public static class ServiceExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        static void ConfigureClient(IServiceProvider sp, HttpClient client)
        {
            var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.GatewayAddress);
        }

        services.AddRefitClient<ICatalogService>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddRefitClient<IBasketApiClient>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddRefitClient<IOrderingService>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddScoped<IBasketService, BasketService>();

        return services;
    }
}
