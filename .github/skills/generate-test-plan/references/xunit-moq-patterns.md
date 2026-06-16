# xUnit + Moq Patterns

Common patterns and code snippets for testing eShop microservices.

## Project Setup

### Test Project Structure

```
src/Services/Ordering/Ordering.Tests/
├── Ordering.Tests.csproj
├── Fixtures/
│   ├── OrderingDbContextFixture.cs
│   └── MessageBusFixture.cs
├── Unit/
│   ├── Orders/
│   │   ├── OrderAggregateTests.cs
│   │   └── OrderValidatorTests.cs
│   └── EventHandlers/
│       └── BasketCheckoutEventHandlerTests.cs
├── Integration/
│   ├── Handlers/
│   │   └── BasketCheckoutEventHandlerTests.cs
│   └── Endpoints/
│       └── CreateOrderEndpointTests.cs
└── Routing/
    └── MessageRoutingTests.cs
```

### .csproj Dependencies

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.8.1" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1" />
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
  <PackageReference Include="Testcontainers" Version="4.2.0" /> <!-- for messaging/DB -->
</ItemGroup>
```

---

## Unit Test Patterns

### 1. Testing Business Logic (Arrange-Act-Assert)

```csharp
public class OrderAggregateTests
{
    [Fact]
    public void AddItem_WithValidProduct_IncreasesTotalPrice()
    {
        // Arrange
        var order = Order.Create("order-123", "customer-456");
        var item = new OrderItem(productId: "prod-1", quantity: 2, unitPrice: 50m);

        // Act
        order.AddItem(item);

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(100m, order.TotalPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithInvalidQuantity_ThrowsException(int quantity)
    {
        // Arrange
        var order = Order.Create("order-123", "customer-456");
        var item = new OrderItem(productId: "prod-1", quantity: quantity, unitPrice: 50m);

        // Act & Assert
        Assert.Throws<DomainException>(() => order.AddItem(item));
    }
}
```

### 2. Testing Validators (FluentValidation + xUnit)

```csharp
public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "cust-123",
            Items = new[] { new OrderItemDto { ProductId = "prod-1", Quantity = 1 } }
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateAsync_WithMissingCustomerId_ReturnsFail(string customerId)
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new[] { new OrderItemDto { ProductId = "prod-1", Quantity = 1 } }
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.CustomerId));
    }
}
```

### 3. Testing Handlers with Mocked Dependencies

```csharp
public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<ILogger<CreateOrderHandler>> _mockLogger;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<CreateOrderHandler>>();
        _handler = new CreateOrderHandler(_mockOrderRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SavesOrderAndReturnsId()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "cust-123",
            Items = new[] { new OrderItemDto { ProductId = "prod-1", Quantity = 2, UnitPrice = 50m } }
        };
        
        Order savedOrder = null;
        _mockOrderRepo
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockOrderRepo.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("cust-123", savedOrder.CustomerId);
    }
}
```

---

## Integration Test Patterns

### 4. Testing Message Handlers (Wolverine)

```csharp
public class BasketCheckoutEventHandlerTests : IAsyncLifetime
{
    private readonly TestWolverineHost _host;
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IBasketService> _mockBasketService;

    public BasketCheckoutEventHandlerTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockBasketService = new Mock<IBasketService>();

        _host = new TestWolverineHost(options =>
        {
            options.Services.AddSingleton(_mockOrderRepo.Object);
            options.Services.AddSingleton(_mockBasketService.Object);
        });
    }

    [Fact]
    public async Task Handle_BasketCheckoutEvent_CreatesOrder()
    {
        // Arrange
        var basketCheckoutEvent = new BasketCheckoutEvent
        {
            BasketId = "basket-123",
            CustomerId = "cust-456",
            Items = new[] { new BasketItemDto { ProductId = "prod-1", Quantity = 2, UnitPrice = 50m } }
        };

        // Act
        await _host.InvokeMessageHandler(basketCheckoutEvent);

        // Assert
        _mockOrderRepo.Verify(
            x => x.AddAsync(It.Is<Order>(o => o.CustomerId == "cust-456"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public Task InitializeAsync() => _host.StartAsync();
    public Task DisposeAsync() => _host.StopAsync();
}
```

### 5. Testing API Endpoints (xUnit + WebApplicationFactory)

```csharp
public class CreateOrderEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly CustomWebApplicationFactory _factory;

    public CreateOrderEndpointTests()
    {
        _factory = new CustomWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateOrder_ReturnsCreated()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "cust-123",
            Items = new[] { new OrderItemDto { ProductId = "prod-1", Quantity = 1, UnitPrice = 50m } }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resultDto = await response.Content.ReadAsAsync<OrderDto>();
        Assert.NotNull(resultDto);
        Assert.Equal("cust-123", resultDto.CustomerId);
    }

    [Fact]
    public async Task Post_CreateOrder_WithMissingItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest { CustomerId = "cust-123", Items = Array.Empty<OrderItemDto>() };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace with test doubles
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(OrderingContext));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<OrderingContext>(options =>
                options.UseInMemoryDatabase("test-db"));
        });
    }
}
```

### 6. Testing External Service Calls (gRPC)

```csharp
public class DiscountServiceClientTests
{
    private readonly Mock<Discount.DiscountClient> _mockGrpcClient;
    private readonly DiscountServiceClient _client;

    public DiscountServiceClientTests()
    {
        _mockGrpcClient = new Mock<Discount.DiscountClient>();
        _client = new DiscountServiceClient(_mockGrpcClient.Object);
    }

    [Fact]
    public async Task GetDiscountByProductName_WithValidProduct_ReturnsDiscount()
    {
        // Arrange
        var productName = "Nike Shoes";
        var expectedDiscount = new GetDiscountResponse { Discount = 20 };

        _mockGrpcClient
            .Setup(x => x.GetDiscountAsync(
                It.Is<GetDiscountRequest>(req => req.ProductName == productName),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDiscount);

        // Act
        var result = await _client.GetDiscountByProductNameAsync(productName);

        // Assert
        Assert.Equal(20, result.Discount);
    }

    [Fact]
    public async Task GetDiscountByProductName_WithUnknownProduct_ReturnsZeroDiscount()
    {
        // Arrange
        var productName = "Unknown Product";
        var expectedDiscount = new GetDiscountResponse { Discount = 0 };

        _mockGrpcClient
            .Setup(x => x.GetDiscountAsync(
                It.Is<GetDiscountRequest>(req => req.ProductName == productName),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDiscount);

        // Act
        var result = await _client.GetDiscountByProductNameAsync(productName);

        // Assert
        Assert.Equal(0, result.Discount);
    }
}
```

---

## Routing Test Patterns

### 7. Testing YARP Gateway Routing

```csharp
public class YarpGatewayRoutingTests : IAsyncLifetime
{
    private readonly HttpClient _gatewayClient;
    private readonly TestServer _backendServer;
    private readonly TestWolverineHost _gatewayHost;

    public YarpGatewayRoutingTests()
    {
        _backendServer = new TestServer(/* backend app builder */);
        _gatewayClient = new HttpClient { BaseAddress = new Uri("http://gateway") };
        // Configure gateway to route to _backendServer
    }

    [Fact]
    public async Task Gateway_RoutesGetRequest_ToBasketService()
    {
        // Arrange
        var expectedResponse = new { basketId = "basket-123", itemCount = 2 };
        
        // Act
        var response = await _gatewayClient.GetAsync("/api/basket/basket-123");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsAsync<object>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task Gateway_EnforcesAuthentication_OnProtectedRoute()
    {
        // Arrange
        _gatewayClient.DefaultRequestHeaders.Authorization = null; // No auth

        // Act
        var response = await _gatewayClient.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;
}
```

### 8. Testing Message Handler Routing (Wolverine)

```csharp
public class MessageHandlerRoutingTests : IAsyncLifetime
{
    private readonly TestWolverineHost _host;

    public MessageHandlerRoutingTests()
    {
        _host = new TestWolverineHost();
    }

    [Fact]
    public async Task BasketCheckoutEvent_IsRoutedToBasketCheckoutEventHandler()
    {
        // Arrange
        var basketCheckoutEvent = new BasketCheckoutEvent { BasketId = "basket-123", CustomerId = "cust-456" };

        // Act
        await _host.InvokeMessageHandler(basketCheckoutEvent);

        // Assert - verify the correct handler was invoked
        var invokedHandler = _host.GetHandledMessages()
            .OfType<BasketCheckoutEvent>()
            .FirstOrDefault(e => e.BasketId == "basket-123");

        Assert.NotNull(invokedHandler);
    }

    public Task InitializeAsync() => _host.StartAsync();
    public Task DisposeAsync() => _host.StopAsync();
}
```

---

## Test Fixture Patterns

### 9. Reusable Fixtures for Database & Messaging

```csharp
public class OrderingDbContextFixture : IAsyncLifetime
{
    private readonly DbContextOptions<OrderingContext> _options;
    public OrderingContext DbContext { get; private set; }

    public OrderingDbContextFixture()
    {
        _options = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: $"test-{Guid.NewGuid()}")
            .Options;
    }

    public async Task InitializeAsync()
    {
        DbContext = new OrderingContext(_options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        DbContext?.Dispose();
    }
}

public class MessageBusFixture : IAsyncLifetime
{
    private TestWolverineHost _host;
    public IMessageBus MessageBus { get; private set; }

    public async Task InitializeAsync()
    {
        _host = new TestWolverineHost();
        await _host.StartAsync();
        MessageBus = _host.GetService<IMessageBus>();
    }

    public async Task DisposeAsync()
    {
        await _host?.StopAsync();
    }
}

public class OrderingTestCollection : ICollectionFixture<OrderingDbContextFixture>, ICollectionFixture<MessageBusFixture>
{
    // Marker class for shared fixtures
}

[Collection(nameof(OrderingTestCollection))]
public class OrderIntegrationTests
{
    private readonly OrderingDbContextFixture _dbFixture;
    private readonly MessageBusFixture _messageBusFixture;

    public OrderIntegrationTests(OrderingDbContextFixture dbFixture, MessageBusFixture messageBusFixture)
    {
        _dbFixture = dbFixture;
        _messageBusFixture = messageBusFixture;
    }

    [Fact]
    public async Task CreateOrder_PersistsToDatabase()
    {
        // Use _dbFixture.DbContext
    }
}
```

---

## Coverage Measurement

### Running Tests with Coverage

```bash
# Run tests and collect coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
```

### Verifying 90% Line Coverage

Update `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Run validation:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line
```
