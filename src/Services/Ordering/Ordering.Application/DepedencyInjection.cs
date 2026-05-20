using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Application
{
    public static class DepedencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            //services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DepedencyInjection).Assembly));

            return services;
        }
    }
}
