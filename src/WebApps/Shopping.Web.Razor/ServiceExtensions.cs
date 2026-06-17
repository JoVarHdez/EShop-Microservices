using Microsoft.Extensions.Options;
using Refit;
using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor;

public static class ServiceExtensions
{
    public static IServiceCollection AddApiClients(
        this IServiceCollection services,
        Func<HttpMessageHandler>? primaryHandlerFactory = null)
    {
        static void ConfigureClient(IServiceProvider sp, HttpClient client)
        {
            var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.GatewayAddress);
        }

        var catalogClient = services.AddRefitClient<ICatalogService>()
            .ConfigureHttpClient(ConfigureClient);

        if (primaryHandlerFactory is not null)
        {
            catalogClient.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        catalogClient.AddStandardResilienceHandler();

        var basketClient = services.AddRefitClient<IBasketApiClient>()
            .ConfigureHttpClient(ConfigureClient);

        if (primaryHandlerFactory is not null)
        {
            basketClient.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        basketClient.AddStandardResilienceHandler();

        var orderingClient = services.AddRefitClient<IOrderingService>()
            .ConfigureHttpClient(ConfigureClient);

        if (primaryHandlerFactory is not null)
        {
            orderingClient.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        orderingClient.AddStandardResilienceHandler();

        services.AddScoped<IBasketService, BasketService>();

        return services;
    }
}
