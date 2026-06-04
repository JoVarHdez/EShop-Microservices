using BuildingBlocks.Exceptions.Handler;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ordering.API.Endpoints;

namespace Ordering.API
{
    public static class DepedencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, ConfigurationManager configuration)
        {
            services.AddExceptionHandler<CustomExceptionHandler>();

            services.AddHealthChecks()
                .AddSqlServer(configuration.GetConnectionString("Database")!);

            return services;
        }

        public static WebApplication UseApiServices(this WebApplication app) 
        {
            app.MapOrdersEndpoints();

            app.UseExceptionHandler(opt => { });

            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }
    }
}
