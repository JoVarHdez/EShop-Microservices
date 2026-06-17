using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;
using Ordering.API.Endpoints;
using Ordering.Application.Data;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Application.Orders.Queries.GetOrderByCustomer;
using Ordering.Application.Orders.Queries.GetOrderByName;
using Ordering.Application.Orders.Queries.GetOrders;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Data.Interceptors;
using Wolverine;
using Wolverine.FluentValidation;

namespace Ordering.Tests.Support;

public sealed class OrderingApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly SqliteConnection? _sqliteConnection;

    private OrderingApiTestHost(WebApplication app, SqliteConnection? sqliteConnection = null)
    {
        _app = app;
        _sqliteConnection = sqliteConnection;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<OrderingApiTestHost> StartAsync(
        Action<ApplicationDbContext>? seed = null,
        IPublishEndpoint? publishEndpoint = null,
        bool orderFulfilmentEnabled = false)
    {
        var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        await sqliteConnection.OpenAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderHandler).Assembly);
        builder.Services.AddScoped<GetOrdersHandler>();
        builder.Services.AddScoped<GetOrderByCustomerHandler>();
        builder.Services.AddScoped<GetOrdersByNameHandler>();
        builder.Services.AddSingleton(publishEndpoint ?? Mock.Of<IPublishEndpoint>());
        builder.Services.AddSingleton(CreateFeatureManager(orderFulfilmentEnabled));
        builder.Services.AddHealthChecks();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(sqliteConnection));
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        builder.Host.UseWolverine(options =>
        {
            options.UseFluentValidation();
            options.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);
        });

        var app = builder.Build();
        app.MapOrdersEndpoints();
        app.MapHealthChecks("/health");

        await InitializeDatabaseAsync(app, seed, ensureCreatedOnly: true);
        await app.StartAsync();

        return new OrderingApiTestHost(app, sqliteConnection);
    }

    public static async Task<OrderingApiTestHost> StartWithSqlServerAsync(
        string connectionString,
        Action<ApplicationDbContext>? seed = null,
        IPublishEndpoint? publishEndpoint = null,
        bool orderFulfilmentEnabled = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderHandler).Assembly);
        builder.Services.AddScoped<GetOrdersHandler>();
        builder.Services.AddScoped<GetOrderByCustomerHandler>();
        builder.Services.AddScoped<GetOrdersByNameHandler>();
        builder.Services.AddSingleton(publishEndpoint ?? Mock.Of<IPublishEndpoint>());
        builder.Services.AddSingleton(CreateFeatureManager(orderFulfilmentEnabled));
        builder.Services.AddHealthChecks().AddSqlServer(connectionString);
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        builder.Services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.AddInterceptors(provider.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        builder.Host.UseWolverine(options =>
        {
            options.UseFluentValidation();
            options.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);
        });

        var app = builder.Build();
        app.MapOrdersEndpoints();
        app.MapHealthChecks("/health");

        await InitializeDatabaseAsync(app, seed, ensureCreatedOnly: false);
        await app.StartAsync();

        return new OrderingApiTestHost(app);
    }

    public static WebApplication BuildAppForRouteInspection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.Services.AddLogging();
        builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderHandler).Assembly);
        builder.Services.AddScoped<GetOrdersHandler>();
        builder.Services.AddScoped<GetOrderByCustomerHandler>();
        builder.Services.AddScoped<GetOrdersByNameHandler>();
        builder.Services.AddSingleton(Mock.Of<IPublishEndpoint>());
        builder.Services.AddSingleton(CreateFeatureManager(false));
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton(Mock.Of<IMessageBus>());
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"ordering-routes-{Guid.NewGuid():N}"));
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var app = builder.Build();
        app.MapOrdersEndpoints();
        app.MapHealthChecks("/health");

        return app;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.DisposeAsync();
        }
        Client.Dispose();
    }

    private static IFeatureManager CreateFeatureManager(bool orderFulfilmentEnabled)
    {
        var featureManager = new Mock<IFeatureManager>();
        featureManager
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>()))
            .ReturnsAsync((string feature) => feature == "OrderFullfilment" && orderFulfilmentEnabled);

        return featureManager.Object;
    }

    private static async Task InitializeDatabaseAsync(WebApplication app, Action<ApplicationDbContext>? seed, bool ensureCreatedOnly)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (ensureCreatedOnly)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        seed?.Invoke(dbContext);
        await dbContext.SaveChangesAsync();
    }
}