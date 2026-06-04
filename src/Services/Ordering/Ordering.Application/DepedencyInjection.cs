using BuildingBlocks.Messaging.MassTransit;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Ordering.Application.Orders.Queries.GetOrderByCustomer;
using Ordering.Application.Orders.Queries.GetOrderByName;
using Ordering.Application.Orders.Queries.GetOrders;
using System.Reflection;

namespace Ordering.Application
{
    public static class DepedencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddScoped<GetOrdersHandler>();
            services.AddScoped<GetOrderByCustomerHandler>();
            services.AddScoped<GetOrdersByNameHandler>();

            services.AddFeatureManagement();
            // Here we set assembly to scan for consumers, sagas, etc. related to MassTransit
            services.AddMessageBroker(configuration, Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
