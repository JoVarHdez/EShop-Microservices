# Test Plan: Catalog API Product Testing

**Spec**: [Catalog API Testing Spec](../specs/20260616-catalogApiTestSpec.md)
**Service**: Catalog.API
**Framework**: xUnit + Moq
**Coverage Target**: 90% line coverage overall, 70%+ branch coverage on critical flows
**Last Updated**: 2026-06-16

## 1. Scope and Objectives

This plan verifies Catalog.API product behavior across validators, Wolverine command handlers, minimal API endpoints, and route registration.

### In Scope

- FluentValidation rule coverage for create, update, and delete commands
- Write-side handler behavior using mocked `IDocumentSession`
- Read-side endpoint behavior using mocked `IQuerySession`
- Command endpoint behavior using mocked `IMessageBus`
- Route registration and route conflict behavior for `/products` and `/products/category/{categoryId}`
- Health endpoint behavior at `/health`

### Out of Scope

- Marten query translation correctness against a real PostgreSQL engine
- Containerized integration environments (no Testcontainers in this plan)
- Load/performance testing

## 2. Test Approach

### 2.1 Unit Tests

Focus on deterministic logic and validation rules without HTTP hosting.

- Validators:
  - `CreateProductCommandValidator`
  - `UpdateProductCommandValidator`
  - `DeleteProductCommandValidator`
- Handlers:
  - `CreateProductCommandHandler`
  - `UpdateProductCommandHandler`
  - `DeleteProductCommandHandler`

### 2.2 Integration Tests (Host-level)

Use `WebApplicationFactory<Program>` and replace dependencies with mocks.

- Replace `IMessageBus` for write endpoints
- Replace `IQuerySession` for read endpoints
- Verify HTTP contracts: status, payload, and route behavior

### 2.3 Routing Tests

Validate endpoint registration and ensure route disambiguation works as expected.

- `/products/{id}` should not capture `/products/category/{categoryId}`
- Health route exists and responds correctly

## 3. Decisions Applied

1. Integration tests use mocks only (Option B): no PostgreSQL Testcontainer
2. Description max length is 250 for both Create and Update validators
3. Unknown category returns 200 OK with an empty collection

## 4. Test Matrix

### 4.1 Unit: Validators

| Area | Test Case | Expected Outcome |
|---|---|---|
| Create validator | Valid command | `IsValid == true` |
| Create validator | Empty name | `Name` validation error |
| Create validator | Empty categories | `Categories` validation error |
| Create validator | Description length 251 | `Description` validation error |
| Create validator | Invalid image url | `ImageUrl` validation error |
| Create validator | Price <= 0 | `Price` validation error |
| Update validator | Valid command | `IsValid == true` |
| Update validator | Empty id | `Id` validation error |
| Update validator | Description length 251 | `Description` validation error |
| Delete validator | Empty product id | `ProductId` validation error |
| Delete validator | Valid product id | `IsValid == true` |

### 4.2 Unit: Command Handlers

| Handler | Test Case | Expected Outcome |
|---|---|---|
| CreateProductCommandHandler | Valid command persists product | `Store` and `SaveChangesAsync` called once; returns non-empty id |
| CreateProductCommandHandler | Property mapping | Stored product matches command data |
| UpdateProductCommandHandler | Product exists | Returns `UpdateProductResult(true)` and persists changes |
| UpdateProductCommandHandler | Product not found | Returns `UpdateProductNotFound`; no update/save calls |
| DeleteProductCommandHandler | Product exists | Returns `DeleteProductResult(true)` and delete/save called |
| DeleteProductCommandHandler | Product not found | Returns `DeleteProductNotFound`; no delete/save calls |

### 4.3 Integration: Endpoints

| Endpoint | Happy Path | Negative/Edge |
|---|---|---|
| `POST /products` | 201 + id + location header | Invalid payload returns 400 and bus not invoked |
| `GET /products` | 200 + product list | Pagination parameters applied correctly |
| `GET /products/{id}` | 200 + product payload | Unknown id returns 404 |
| `GET /products/category/{categoryId}` | 200 + matching products | Unknown category returns 200 + empty list |
| `PUT /products/{id}` | 200 + `isSuccess=true` | Validation returns 400; not found returns 404 |
| `DELETE /products/{id}` | 200 + `isSuccess=true` | Unknown id returns 404 |

### 4.4 Routing

| Test Case | Expected Outcome |
|---|---|
| Product routes registered | All six product routes are present |
| Category route disambiguation | `/products/category/{categoryId}` resolves to category endpoint |
| Health route | `/health` returns healthy status when dependencies are healthy |

## 5. Test Class Layout

```
test/Catalog.Tests/
  Unit/
    Validators/
      CreateProductCommandValidatorTests.cs
      UpdateProductCommandValidatorTests.cs
      DeleteProductCommandValidatorTests.cs
    Handlers/
      CreateProductCommandHandlerTests.cs
      UpdateProductCommandHandlerTests.cs
      DeleteProductCommandHandlerTests.cs
  Integration/
    Endpoints/
      CreateProductEndpointTests.cs
      GetProductsEndpointTests.cs
      GetProductByIdEndpointTests.cs
      GetProductByCategoryEndpointTests.cs
      UpdateProductEndpointTests.cs
      DeleteProductEndpointTests.cs
  Routing/
    ProductsRoutingTests.cs
```

## 6. Execution and Verification

For local execution details (including Docker-backed coverage and troubleshooting), see:

- [Catalog.Tests local runbook](../../test/Catalog.Tests/README.md)

### Suggested Commands

```powershell
dotnet test test/Catalog.Tests/Catalog.Tests.csproj
```

```powershell
dotnet test test/Catalog.Tests/Catalog.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Exit Criteria

- All planned test cases are implemented and passing
- Overall line coverage is >= 90%
- Critical branch coverage is >= 70%
- Both not-found branches are covered in update and delete handlers
- Route conflict test proves category and id routes are distinct

## 7. Risks and Mitigations

- Risk: Mock-only integration tests may miss Marten provider issues
  - Mitigation: Add a separate optional smoke suite later against a real database
- Risk: Route changes can silently break endpoint matching
  - Mitigation: Keep routing tests mandatory in CI
- Risk: Validation changes can bypass command dispatch assumptions
  - Mitigation: Keep assertions that `IMessageBus` is not called on invalid requests
