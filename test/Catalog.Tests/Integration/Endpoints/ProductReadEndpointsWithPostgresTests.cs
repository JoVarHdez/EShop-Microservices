using System.Net;
using System.Net.Http.Json;
using Catalog.API.Models;
using Catalog.API.Products.GetProductByCategory;
using Catalog.API.Products.GetProducts;
using Catalog.Tests.Support;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testcontainers.PostgreSql;
using Wolverine;

namespace Catalog.Tests.Integration.Endpoints;

[Collection("CatalogPostgresRead")]
public class ProductReadEndpointsWithPostgresTests : IAsyncLifetime
{
    private const string RequireDockerEnvVar = "CATALOG_REQUIRE_DOCKER_TESTS";
    private PostgreSqlContainer? _postgres;
    private bool _dockerAvailable = true;
    private string _dockerUnavailableReason = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("catalog")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
        }
        catch (Exception ex)
        {
            _dockerAvailable = false;
            _dockerUnavailableReason = ex.Message;
        }
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetProducts_WithDefaultPagination_ReturnsOkAndProducts()
    {
        if (!EnsureDockerAvailable()) return;

        var bus = new Mock<Wolverine.IMessageBus>();
        await using var host = await ProductApiTestHost.StartWithMartenAsync(_postgres!.GetConnectionString(), bus.Object);

        await ResetAndSeedAsync(host.Services,
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Phone A",
                Categories = ["Electronics"],
                Description = "D1",
                ImageUrl = "https://example.com/a.png",
                Price = 100m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Phone B",
                Categories = ["Electronics"],
                Description = "D2",
                ImageUrl = "https://example.com/b.png",
                Price = 150m
            });

        var response = await host.Client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetProductsResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Products.Any());
    }

    [Fact]
    public async Task GetProductByCategory_WithUnknownCategory_ReturnsOkAndEmptyList()
    {
        if (!EnsureDockerAvailable()) return;

        var bus = new Mock<Wolverine.IMessageBus>();
        await using var host = await ProductApiTestHost.StartWithMartenAsync(_postgres!.GetConnectionString(), bus.Object);

        await ResetAndSeedAsync(host.Services,
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Categories = ["Computers"],
                Description = "D",
                ImageUrl = "https://example.com/laptop.png",
                Price = 999m
            });

        var response = await host.Client.GetAsync("/products/category/UnknownCategory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetProductsByCategoryResponse>();
        Assert.NotNull(body);
        Assert.Empty(body!.Products);
    }

    [Fact]
    public async Task GetProductByCategory_WithKnownCategory_ReturnsMatchingProducts()
    {
        if (!EnsureDockerAvailable()) return;

        var bus = new Mock<Wolverine.IMessageBus>();
        await using var host = await ProductApiTestHost.StartWithMartenAsync(_postgres!.GetConnectionString(), bus.Object);

        await ResetAndSeedAsync(host.Services,
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Categories = ["Electronics", "Mobile"],
                Description = "D",
                ImageUrl = "https://example.com/phone.png",
                Price = 500m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Chair",
                Categories = ["Furniture"],
                Description = "D",
                ImageUrl = "https://example.com/chair.png",
                Price = 80m
            });

        var response = await host.Client.GetAsync("/products/category/Electronics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetProductsByCategoryResponse>();
        Assert.NotNull(body);
        Assert.All(body!.Products, p => Assert.Contains("Electronics", p.Categories));
        Assert.Single(body.Products);
    }

    private static async Task ResetAndSeedAsync(IServiceProvider services, params Product[] products)
    {
        await using var scope = services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        await using var session = store.LightweightSession();
        session.DeleteWhere<Product>(_ => true);
        session.Store(products);
        await session.SaveChangesAsync();
    }

    private bool EnsureDockerAvailable()
    {
        if (!_dockerAvailable)
        {
            var requireDocker = string.Equals(
                Environment.GetEnvironmentVariable(RequireDockerEnvVar),
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (requireDocker)
            {
                Assert.Fail($"Docker is required for container-backed tests, but is unavailable: {_dockerUnavailableReason}");
            }

            return false;
        }

        return true;
    }
}
