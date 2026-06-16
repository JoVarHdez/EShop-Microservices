using System.Net;
using System.Net.Http.Json;
using System.Text;
using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.DeleteProduct;
using Catalog.API.Products.UpdateProduct;
using Catalog.Tests.Support;
using Marten;
using Marten.Linq;
using Moq;
using Wolverine;

namespace Catalog.Tests.Integration.Endpoints;

public class ProductEndpointsTests
{
    [Fact]
    public async Task CreateProduct_WithValidRequest_ReturnsCreated()
    {
        var expectedId = Guid.NewGuid();
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<CreateProductResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateProductResult(expectedId));

        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.PostAsJsonAsync("/products", new
        {
            name = "IPhone X",
            categories = new[] { "Smart Phone" },
            description = "Flagship phone",
            imageUrl = "https://example.com/product.png",
            price = 999
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/products/{expectedId}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CreateProduct_WithMalformedJson_ReturnsBadRequest()
    {
        var bus = new Mock<IMessageBus>();
        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var content = new StringContent("{", Encoding.UTF8, "application/json");
        var response = await host.Client.PostAsync("/products", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithExistingProduct_ReturnsOk()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<UpdateProductCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateProductResult(true));

        await using var host = await ProductApiTestHost.StartAsync(bus.Object);
        var id = Guid.NewGuid();

        var response = await host.Client.PutAsJsonAsync($"/products/{id}", new
        {
            id,
            name = "Updated",
            categories = new[] { "Smart Phone" },
            description = "Updated description",
            imageUrl = "https://example.com/updated.png",
            price = 1000
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithNotFoundResult_ReturnsNotFound()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<UpdateProductCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateProductNotFound());

        await using var host = await ProductApiTestHost.StartAsync(bus.Object);
        var id = Guid.NewGuid();

        var response = await host.Client.PutAsJsonAsync($"/products/{id}", new
        {
            id,
            name = "Updated",
            categories = new[] { "Smart Phone" },
            description = "Updated description",
            imageUrl = "https://example.com/updated.png",
            price = 1000
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingProduct_ReturnsOk()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<DeleteProductCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteProductResult(true));

        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.DeleteAsync($"/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithNotFoundResult_ReturnsNotFound()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.InvokeAsync<DeleteProductCommandResult>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteProductNotFound());

        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.DeleteAsync($"/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_WithExistingProduct_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var bus = new Mock<IMessageBus>();
        var querySession = new Mock<IQuerySession>();
        querySession.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = productId,
                Name = "IPhone X",
                Categories = ["Smart Phone"],
                Description = "Flagship",
                ImageUrl = "https://example.com/product.png",
                Price = 999m
            });

        await using var host = await ProductApiTestHost.StartAsync(bus.Object, querySession.Object);

        var response = await host.Client.GetAsync($"/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_WithUnknownId_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();
        var bus = new Mock<IMessageBus>();
        var querySession = new Mock<IQuerySession>();
        querySession.Setup(x => x.LoadAsync<Product>(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await using var host = await ProductApiTestHost.StartAsync(bus.Object, querySession.Object);

        var response = await host.Client.GetAsync($"/products/{productId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithMalformedPageNumber_ReturnsBadRequest()
    {
        var bus = new Mock<IMessageBus>();
        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.GetAsync("/products?pageNumber=abc&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_WithMalformedGuid_ReturnsBadRequest()
    {
        var bus = new Mock<IMessageBus>();
        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.GetAsync("/products/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProductByCategory_WithMissingCategorySegment_ReturnsBadRequest()
    {
        var bus = new Mock<IMessageBus>();
        await using var host = await ProductApiTestHost.StartAsync(bus.Object);

        var response = await host.Client.GetAsync("/products/category");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithMockQueryProvider_ThrowsInvalidCastException()
    {
        var bus = new Mock<IMessageBus>();
        var querySession = new Mock<IQuerySession>();
        querySession.Setup(x => x.Query<Product>())
            .Returns(new Mock<IMartenQueryable<Product>>().Object);

        await using var host = await ProductApiTestHost.StartAsync(bus.Object, querySession.Object);

        var ex = await Record.ExceptionAsync(() => host.Client.GetAsync("/products?pageNumber=1&pageSize=10"));

        Assert.NotNull(ex);
        Assert.True(ex is InvalidCastException or NullReferenceException);
    }

    [Fact]
    public async Task GetProductByCategory_WithMockQueryProvider_ThrowsInvalidCastException()
    {
        var bus = new Mock<IMessageBus>();
        var querySession = new Mock<IQuerySession>();
        querySession.Setup(x => x.Query<Product>())
            .Returns(new Mock<IMartenQueryable<Product>>().Object);

        await using var host = await ProductApiTestHost.StartAsync(bus.Object, querySession.Object);

        var ex = await Record.ExceptionAsync(() => host.Client.GetAsync("/products/category/UnknownCategory"));

        Assert.NotNull(ex);
        Assert.True(ex is InvalidCastException or NullReferenceException);
    }
}
