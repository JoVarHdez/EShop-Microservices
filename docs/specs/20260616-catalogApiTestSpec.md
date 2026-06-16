# Test Plan: Catalog API — Product CRUD Testing

**Spec**: [Catalog API Wolverine Modernization](./20260515-catalogApiWolverineModernization.md)
**Service**: `Catalog.API`
**Framework**: xUnit + Moq
**Coverage Target**: 90% line coverage, 70%+ branch coverage on critical paths
**Last Updated**: 2026-06-16

---

## Overview

The Catalog API is a .NET 10 minimal API microservice that manages the product catalog for the eShop platform. It uses Wolverine for command dispatch, Marten (PostgreSQL) for persistence, and FluentValidation for input validation.

**Key responsibilities under test:**

- Product CRUD operations exposed via 6 REST endpoints under `/products`
- Write-side Wolverine command handlers (`CreateProduct`, `UpdateProduct`, `DeleteProduct`)
- Read-side queries inlined directly in endpoint lambdas via Marten `IQuerySession`
- FluentValidation rules applied as Wolverine middleware before each command handler
- Discriminated union result patterns for update/delete (`NotFound` vs. success result)
- Health check endpoint at `/health`

**External dependencies:**

| Dependency | Role | Test Strategy |
|---|---|---|
| `IDocumentSession` (Marten) | Write session for create/update/delete | Moq |
| `IQuerySession` (Marten) | Read-only session for get endpoints | Moq |
| `IMessageBus` (Wolverine) | Command dispatch in command endpoints | Moq |
| PostgreSQL | Persistence backing store | Moq (mocked via `IDocumentSession`/`IQuerySession`) |

---

## Test Project Structure

```
test/Catalog.Tests/
├── Catalog.Tests.csproj
├── Unit/
│   ├── Validators/
│   │   ├── CreateProductCommandValidatorTests.cs
│   │   ├── UpdateProductCommandValidatorTests.cs
│   │   └── DeleteProductCommandValidatorTests.cs
│   └── Handlers/
│       ├── CreateProductCommandHandlerTests.cs
│       ├── UpdateProductCommandHandlerTests.cs
│       └── DeleteProductCommandHandlerTests.cs
├── Integration/
│   └── Endpoints/
│       ├── CreateProductEndpointTests.cs
│       ├── GetProductsEndpointTests.cs
│       ├── GetProductByIdEndpointTests.cs
│       ├── GetProductByCategoryEndpointTests.cs
│       ├── UpdateProductEndpointTests.cs
│       └── DeleteProductEndpointTests.cs
└── Routing/
    └── ProductsRoutingTests.cs
```

### .csproj Dependencies

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.9.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.9.0" />
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <ProjectReference Include="..\..\src\Services\Catalog\Catalog.API\Catalog.API.csproj" />
</ItemGroup>
```

---

## Unit Tests

### Category: FluentValidation — `CreateProductCommandValidator`

#### Test Case: Valid command passes validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithValidCommand_ReturnsSuccess`
- **Arrangement**: Build a `CreateProductCommand` with all fields valid (non-empty name, at least one category, description ≤ 250 chars, valid URI image URL, price > 0)
- **Action**: Call `validator.ValidateAsync(command)`
- **Assertion**: `result.IsValid == true`, zero errors

```csharp
[Fact]
public async Task ValidateAsync_WithValidCommand_ReturnsSuccess()
{
    var validator = new CreateProductCommandValidator();
    var command = new CreateProductCommand(
        Name: "IPhone X",
        Categories: ["Smart Phone"],
        Description: "This phone is the company's biggest change to its flagship smartphone in years.",
        ImageUrl: "https://www.eshopstore.com/product-1.png",
        Price: 950.00m
    );

    var result = await validator.ValidateAsync(command);

    Assert.True(result.IsValid);
}
```

---

#### Test Case: Empty name fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithEmptyName_ReturnsNameError`
- **Arrangement**: `CreateProductCommand` with `Name = ""`
- **Action**: Call `validator.ValidateAsync(command)`
- **Assertion**: `result.IsValid == false`; error for `Name` property with message `"Name is required."`

---

#### Test Case: Empty categories list fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithEmptyCategories_ReturnsCategoriesError`
- **Arrangement**: `CreateProductCommand` with `Categories = []`
- **Assertion**: Error on `Categories` property, message `"At least one category is required."`

---

#### Test Case: Description exceeds 250 characters fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithDescriptionOver250Chars_ReturnsDescriptionError`
- **Arrangement**: `Description` = string of 251 characters
- **Assertion**: Error on `Description` property

---

#### Test Case: Invalid image URL fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithInvalidImageUrl_ReturnsImageUrlError`
- **Arrangement**: `ImageUrl = "not a url at all $$##"`
- **Assertion**: Error on `ImageUrl` property, message `"ImageUrl must be a valid URL."`

---

#### Test Case: Price of zero fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithZeroPrice_ReturnsPriceError`
- **Arrangement**: `Price = 0m`
- **Assertion**: Error on `Price` property, message `"Price must be greater than 0."`

#### Test Case: Negative price fails validation
- **Class**: `CreateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithNegativePrice_ReturnsPriceError`
- **Arrangement**: `Price = -1m`
- **Assertion**: Error on `Price` property

---

### Category: FluentValidation — `UpdateProductCommandValidator`

#### Test Case: Valid command passes validation
- **Class**: `UpdateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithValidCommand_ReturnsSuccess`
- **Arrangement**: Full valid `UpdateProductCommand` including a non-empty `Id`
- **Assertion**: `result.IsValid == true`

---

#### Test Case: Empty Id fails validation
- **Class**: `UpdateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithEmptyId_ReturnsIdError`
- **Arrangement**: `Id = Guid.Empty`
- **Assertion**: Error on `Id` property, message `"Id is required."`

---

#### Test Case: Description exceeds 250 characters fails validation
- **Class**: `UpdateProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithDescriptionOver250Chars_ReturnsDescriptionError`
- **Arrangement**: `Description` = string of 251 characters
- **Assertion**: Error on `Description` property

---

### Category: FluentValidation — `DeleteProductCommandValidator`

#### Test Case: Empty ProductId fails validation
- **Class**: `DeleteProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithEmptyProductId_ReturnsProductIdError`
- **Arrangement**: `DeleteProductCommand(ProductId: Guid.Empty)`
- **Assertion**: Error on `ProductId` property, message `"Product ID is required."`

#### Test Case: Valid ProductId passes validation
- **Class**: `DeleteProductCommandValidatorTests`
- **Method**: `ValidateAsync_WithValidProductId_ReturnsSuccess`
- **Arrangement**: `DeleteProductCommand(ProductId: Guid.NewGuid())`
- **Assertion**: `result.IsValid == true`

---

### Category: Command Handler — `CreateProductCommandHandler`

#### Test Case: Valid command stores product and returns Id
- **Class**: `CreateProductCommandHandlerTests`
- **Method**: `Handle_WithValidCommand_StoresProductAndReturnsId`
- **Arrangement**:
  - Mock `IDocumentSession`
  - Stub `session.Store<Product>(product)` (void)
  - Stub `session.SaveChangesAsync(ct)` → `Task.CompletedTask`
- **Action**: Call `handler.Handle(command, CancellationToken.None)`
- **Assertion**:
  - Result is `CreateProductResult` with non-empty `Id`
  - `session.Store<Product>()` was called exactly once
  - `session.SaveChangesAsync()` was called exactly once
- **Dependencies to Mock**: `IDocumentSession`

```csharp
[Fact]
public async Task Handle_WithValidCommand_StoresProductAndReturnsId()
{
    // Arrange
    var mockSession = new Mock<IDocumentSession>();
    mockSession.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

    var handler = new CreateProductCommandHandler(mockSession.Object);
    var command = new CreateProductCommand(
        Name: "IPhone X",
        Categories: ["Smart Phone"],
        Description: "Flagship smartphone.",
        ImageUrl: "https://example.com/img.png",
        Price: 950.00m
    );

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.IsType<CreateProductResult>(result);
    Assert.NotEqual(Guid.Empty, result.Id);
    mockSession.Verify(s => s.Store(It.IsAny<Product>()), Times.Once);
    mockSession.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

---

#### Test Case: Product properties are mapped from command
- **Class**: `CreateProductCommandHandlerTests`
- **Method**: `Handle_WithValidCommand_MapsAllPropertiesFromCommand`
- **Arrangement**: Mock `IDocumentSession`; capture the `Product` argument passed to `Store()`
- **Action**: Call handler
- **Assertion**: Captured product's `Name`, `Categories`, `Description`, `ImageUrl`, and `Price` match the command values

---

### Category: Command Handler — `UpdateProductCommandHandler`

#### Test Case: Existing product is updated and returns success result
- **Class**: `UpdateProductCommandHandlerTests`
- **Method**: `Handle_WithExistingProduct_UpdatesProductAndReturnsSuccess`
- **Arrangement**:
  - Mock `IDocumentSession`
  - Stub `session.LoadAsync<Product>(id, ct)` → returns a product
  - Stub `session.SaveChangesAsync(ct)` → `Task.CompletedTask`
- **Action**: Call handler with `UpdateProductCommand`
- **Assertion**:
  - Result is `UpdateProductResult` with `IsSuccess == true`
  - `session.Update(product)` called once with updated values
  - `session.SaveChangesAsync()` called once
- **Dependencies to Mock**: `IDocumentSession`

---

#### Test Case: Non-existent product returns NotFound result
- **Class**: `UpdateProductCommandHandlerTests`
- **Method**: `Handle_WithNonExistentProduct_ReturnsNotFoundResult`
- **Arrangement**:
  - Mock `IDocumentSession`
  - Stub `session.LoadAsync<Product>(id, ct)` → returns `null`
- **Action**: Call handler
- **Assertion**:
  - Result is `UpdateProductNotFound`
  - `session.Update()` was **never** called
  - `session.SaveChangesAsync()` was **never** called

```csharp
[Fact]
public async Task Handle_WithNonExistentProduct_ReturnsNotFoundResult()
{
    // Arrange
    var productId = Guid.NewGuid();
    var mockSession = new Mock<IDocumentSession>();
    mockSession.Setup(s => s.LoadAsync<Product>(productId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Product?)null);

    var handler = new UpdateProductCommandHandler(mockSession.Object);
    var command = new UpdateProductCommand(
        Id: productId, Name: "Updated", Categories: ["Cat"],
        Description: "Desc", ImageUrl: "https://example.com/img.png", Price: 10m
    );

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.IsType<UpdateProductNotFound>(result);
    mockSession.Verify(s => s.Update(It.IsAny<Product>()), Times.Never);
    mockSession.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
```

---

#### Test Case: All fields are updated on the product entity
- **Class**: `UpdateProductCommandHandlerTests`
- **Method**: `Handle_WithValidCommand_UpdatesAllFieldsOnProduct`
- **Arrangement**: Stub `LoadAsync` to return a product; capture the product passed to `Update()`
- **Assertion**: Updated product's `Name`, `Description`, `Categories`, `ImageUrl`, `Price` all match command values

---

### Category: Command Handler — `DeleteProductCommandHandler`

#### Test Case: Existing product is deleted and returns success result
- **Class**: `DeleteProductCommandHandlerTests`
- **Method**: `Handle_WithExistingProduct_DeletesProductAndReturnsSuccess`
- **Arrangement**:
  - Mock `IDocumentSession`
  - Stub `LoadAsync<Product>(id, ct)` → returns a product
  - Stub `SaveChangesAsync(ct)` → `Task.CompletedTask`
- **Action**: Call handler with `DeleteProductCommand`
- **Assertion**:
  - Result is `DeleteProductResult` with `IsSuccess == true`
  - `session.Delete<Product>(id)` called once
  - `session.SaveChangesAsync()` called once

---

#### Test Case: Non-existent product returns NotFound result
- **Class**: `DeleteProductCommandHandlerTests`
- **Method**: `Handle_WithNonExistentProduct_ReturnsNotFoundResult`
- **Arrangement**:
  - Mock `IDocumentSession`
  - Stub `LoadAsync<Product>(id, ct)` → returns `null`
- **Action**: Call handler
- **Assertion**:
  - Result is `DeleteProductNotFound`
  - `session.Delete<Product>()` was **never** called
  - `session.SaveChangesAsync()` was **never** called

---

## Integration Tests

> All integration tests use `WebApplicationFactory<Program>` and mock `IDocumentSession`/`IQuerySession`/`IMessageBus` via service replacement unless a Testcontainer is spun up.

### API Endpoint Integration Matrix

| Endpoint | Method | Route | Handler / Query | Success Status | Error Scenarios |
|---|---|---|---|---|---|
| CreateProduct | POST | `/products` | `CreateProductCommandHandler` (via Wolverine) | 201 Created | 400 invalid body |
| GetProducts | GET | `/products` | Inline `IQuerySession` | 200 OK | 400 bad page params |
| GetProductById | GET | `/products/{id}` | Inline `IQuerySession` | 200 OK | 404 Not Found |
| GetProductByCategory | GET | `/products/category/{categoryId}` | Inline `IQuerySession` | 200 OK | empty list |
| UpdateProduct | PUT | `/products/{id}` | `UpdateProductCommandHandler` (via Wolverine) | 200 OK | 400, 404 |
| DeleteProduct | DELETE | `/products/{id}` | `DeleteProductCommandHandler` (via Wolverine) | 200 OK | 404 |

---

### Endpoint: `POST /products`

#### Test Case: Valid request returns 201 with product Id
- **Class**: `CreateProductEndpointTests`
- **Method**: `CreateProduct_WithValidRequest_Returns201AndProductId`
- **Setup**:
  - Replace `IMessageBus` with mock that returns `new CreateProductResult(Guid.NewGuid())`
- **Action**: `POST /products` with valid JSON body
- **Assertion**:
  - Response status = `201 Created`
  - `Location` header = `/products/{newId}`
  - Response body contains `id` field (non-empty GUID)

```csharp
[Fact]
public async Task CreateProduct_WithValidRequest_Returns201AndProductId()
{
    // Arrange
    var expectedId = Guid.NewGuid();
    var mockBus = new Mock<IMessageBus>();
    mockBus.Setup(b => b.InvokeAsync<CreateProductResult>(
               It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new CreateProductResult(expectedId));

    using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddSingleton(mockBus.Object)));
    var client = factory.CreateClient();

    var request = new
    {
        name = "IPhone X",
        categories = new[] { "Smart Phone" },
        description = "Flagship phone.",
        imageUrl = "https://example.com/img.png",
        price = 950.00
    };

    // Act
    var response = await client.PostAsJsonAsync("/products", request);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
    Assert.Equal(expectedId, body!.Id);
}
```

---

#### Test Case: Invalid request returns 400 with validation problem details
- **Class**: `CreateProductEndpointTests`
- **Method**: `CreateProduct_WithMissingName_Returns400ValidationError`
- **Setup**: Mock `IMessageBus` (should never be invoked)
- **Action**: `POST /products` with `name = ""`, all other fields valid
- **Assertion**:
  - Response status = `400 Bad Request`
  - Response body is `ProblemDetails` with `errors` containing `Name`
  - `IMessageBus.InvokeAsync` was **never** called (validation stops dispatch)

---

### Endpoint: `GET /products`

#### Test Case: Returns paginated product list
- **Class**: `GetProductsEndpointTests`
- **Method**: `GetProducts_WithDefaultPaging_Returns200AndProducts`
- **Setup**: Replace `IQuerySession` with mock; stub query to return 2 sample products
- **Action**: `GET /products`
- **Assertion**:
  - Response status = `200 OK`
  - Body contains `products` array with 2 items
  - Each product has `id`, `name`, `price`, `categories`, `imageUrl`, `description`

---

#### Test Case: Custom page size is respected
- **Class**: `GetProductsEndpointTests`
- **Method**: `GetProducts_WithCustomPageSize_UsesProvidedPagingValues`
- **Setup**: Stub query session paginated response
- **Action**: `GET /products?pageNumber=2&pageSize=5`
- **Assertion**: Query is executed with `pageNumber=2`, `pageSize=5`

---

### Endpoint: `GET /products/{id}`

#### Test Case: Known product Id returns 200 with product details
- **Class**: `GetProductByIdEndpointTests`
- **Method**: `GetProductById_WithValidId_Returns200AndProduct`
- **Setup**: Mock `IQuerySession`; stub `LoadAsync<Product>(id)` → returns a product
- **Action**: `GET /products/{id}`
- **Assertion**: `200 OK`; body `product` object matches expected fields

---

#### Test Case: Unknown product Id returns 404
- **Class**: `GetProductByIdEndpointTests`
- **Method**: `GetProductById_WithUnknownId_Returns404`
- **Setup**: Mock `IQuerySession`; stub `LoadAsync<Product>(id)` → returns `null`
- **Action**: `GET /products/{unknownId}`
- **Assertion**: `404 Not Found`

---

### Endpoint: `GET /products/category/{categoryId}`

#### Test Case: Known category returns matching products
- **Class**: `GetProductByCategoryEndpointTests`
- **Method**: `GetProductByCategory_WithKnownCategory_Returns200AndProducts`
- **Setup**: Mock `IQuerySession`; stub query filtered by `categoryId` → returns 2 products
- **Action**: `GET /products/category/Smart%20Phone`
- **Assertion**: `200 OK`; `products` array contains only products with `"Smart Phone"` in their categories

---

#### Test Case: No products for category returns empty list (not 404)
- **Class**: `GetProductByCategoryEndpointTests`
- **Method**: `GetProductByCategory_WithUnknownCategory_Returns200AndEmptyList`
- **Setup**: Stub query → returns empty list
- **Action**: `GET /products/category/NonExistentCategory`
- **Assertion**: `200 OK`; `products` array is empty

---

### Endpoint: `PUT /products/{id}`

#### Test Case: Valid update returns 200 with success response
- **Class**: `UpdateProductEndpointTests`
- **Method**: `UpdateProduct_WithExistingProduct_Returns200`
- **Setup**: Mock `IMessageBus`; `InvokeAsync` returns `UpdateProductResult(IsSuccess: true)`
- **Action**: `PUT /products/{id}` with valid body
- **Assertion**: `200 OK`; body `isSuccess == true`

---

#### Test Case: Update on non-existent product returns 404
- **Class**: `UpdateProductEndpointTests`
- **Method**: `UpdateProduct_WithNonExistentProduct_Returns404`
- **Setup**: Mock `IMessageBus`; `InvokeAsync` returns `UpdateProductNotFound`
- **Action**: `PUT /products/{id}` with valid body
- **Assertion**: `404 Not Found`

---

#### Test Case: Invalid request body returns 400
- **Class**: `UpdateProductEndpointTests`
- **Method**: `UpdateProduct_WithInvalidBody_Returns400`
- **Setup**: Mock `IMessageBus` (should not be called)
- **Action**: `PUT /products/{id}` with `price = -5`, `name = ""`
- **Assertion**: `400 Bad Request`; `IMessageBus` not invoked

---

### Endpoint: `DELETE /products/{id}`

#### Test Case: Existing product is deleted, returns 200
- **Class**: `DeleteProductEndpointTests`
- **Method**: `DeleteProduct_WithExistingProduct_Returns200`
- **Setup**: Mock `IMessageBus`; returns `DeleteProductResult(IsSuccess: true)`
- **Action**: `DELETE /products/{id}`
- **Assertion**: `200 OK`; body `isSuccess == true`

---

#### Test Case: Non-existent product returns 404
- **Class**: `DeleteProductEndpointTests`
- **Method**: `DeleteProduct_WithNonExistentProduct_Returns404`
- **Setup**: Mock `IMessageBus`; returns `DeleteProductNotFound`
- **Action**: `DELETE /products/{unknownId}`
- **Assertion**: `404 Not Found`

---

## Routing Tests

### API Endpoint Routing Matrix

| Method | Route | Handler / Target | Expected Status (Happy Path) | Expected Status (Error) |
|---|---|---|---|---|
| `POST` | `/products` | `CreateProductCommandHandler` | 201 | 400 (validation) |
| `GET` | `/products` | Inline `IQuerySession` | 200 | 400 (invalid params) |
| `GET` | `/products/{id}` | Inline `IQuerySession` | 200 | 404 |
| `GET` | `/products/category/{categoryId}` | Inline `IQuerySession` | 200 | 200 + empty list |
| `PUT` | `/products/{id}` | `UpdateProductCommandHandler` | 200 | 400, 404 |
| `DELETE` | `/products/{id}` | `DeleteProductCommandHandler` | 200 | 404 |
| `GET` | `/health` | ASP.NET HealthChecks | 200 | 503 (DB unreachable) |

---

### Test Case: All product routes are registered at /products group
- **Class**: `ProductsRoutingTests`
- **Method**: `MapProductsEndpoints_RegistersAllSixRoutes`
- **Arrangement**: Build a `WebApplication` with minimal service setup; call `app.MapProductsEndpoints()`
- **Assertion**: The endpoint route table contains entries for all 6 routes above

---

### Test Case: GET /products/category/{id} does not conflict with GET /products/{id}
- **Class**: `ProductsRoutingTests`
- **Method**: `GetProductByCategory_DoesNotConflictWithGetProductById`
- **Arrangement**: Two endpoints registered under the same group prefix `/products`
- **Action**: `GET /products/category/Electronics`
- **Assertion**: Request routed to `GetProductByCategory` handler, NOT `GetProductById`

---

### Test Case: Health check endpoint is reachable
- **Class**: `ProductsRoutingTests`
- **Method**: `HealthCheck_Endpoint_Returns200_WhenHealthy`
- **Setup**: Mock `IHealthCheck` or ensure DB connection is healthy
- **Action**: `GET /health`
- **Assertion**: `200 OK`; response body is valid health check JSON

---

## Code Coverage Targets

| Area | Target | Notes |
|---|---|---|
| `CreateProductCommandHandler` | 100% line | Simple handler, full coverage expected |
| `UpdateProductCommandHandler` | 100% line | Both branches (found / not found) must be hit |
| `DeleteProductCommandHandler` | 100% line | Both branches (found / not found) must be hit |
| `CreateProductCommandValidator` | 90%+ | All rules: Name, Categories, Description, ImageUrl, Price |
| `UpdateProductCommandValidator` | 90%+ | All rules including Id |
| `DeleteProductCommandValidator` | 90%+ | ProductId rule |
| Endpoint lambdas (read) | 80%+ | Query inline code — null check in `GetProductById` is critical |
| Endpoint lambdas (write) | 90%+ | Discriminated union switch arms must all be covered |
| Overall service | **90% line** | Excludes auto-generated code and `CatalogInitialData` seeding |

---

## Acceptance Criteria

The test plan is complete when:

- [ ] All unit tests for 3 validators pass (positive + all negative cases)
- [ ] All unit tests for 3 command handlers pass (success + not-found branches)
- [ ] All 6 endpoint integration tests pass (happy path + at least one error per endpoint)
- [ ] Routing test confirms `/products/category/{id}` does not conflict with `/products/{id}`
- [ ] Health check endpoint test passes
- [ ] `dotnet test` reports ≥ 90% line coverage for `Catalog.API` (excluding seed data)
- [ ] No test references `IDocumentSession` or `IQuerySession` directly against PostgreSQL in unit tests (Moq only)
- [ ] FluentValidation error tests confirm `IMessageBus` is never invoked when validation fails

---

## Decisions

1. **Integration test isolation** → **Mock only (Option B)**: `IDocumentSession`, `IQuerySession`, and `IMessageBus` are replaced via `WebApplicationFactory` service overrides. No Testcontainers dependency. Marten query translation is out of scope for this test suite.

2. **Description max length** → **250 characters for both Create and Update**: The 500-character limit in `UpdateProductCommandValidator` was a bug. `UpdateProductHandler.cs` has been corrected to `MaximumLength(250)`. The description-over-250 test case applies equally to both validators.

3. **Unknown category response** → **200 OK + empty list (collection semantics)**: `GET /products/category/{categoryId}` returns `200 OK` with an empty `products` array when no products match. This is the recommended behaviour for collection endpoints.
