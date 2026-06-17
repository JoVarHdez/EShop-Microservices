# Test Plan: Ordering API Testing

**Spec**: [Ordering Test Specification](../specs/20260617-orderingApiTestSpec.md)
**Service**: Ordering
**Framework**: xUnit + Moq
**Coverage Target**: 90% line coverage, 70%+ branch coverage on critical paths
**Last Updated**: 2026-06-17

## 1. Overview

This plan implements the first dedicated automated test suite for Ordering across unit, routing, host-level integration, and minimal SQL Server-backed provider verification. The suite must cover both HTTP entry points and the Basket checkout consumer path because Ordering creates orders through both routes.

The plan follows the current repository testing shape used by Basket and Catalog:
- one service-owned test project under `test/Ordering.Tests/`
- xUnit + Moq for mock-solvable coverage
- `UseTestServer()` host harness for route and endpoint verification
- one Docker-backed SQL Server slice for provider-coupled persistence and health checks

### Decisions Applied

1. CI policy is deferred and is not part of this plan.
2. Provider baseline is SQL Server in Docker/Testcontainers.
3. Domain event publication must be asserted in at least one end-to-end create or update integration path.

## 2. Relevant Files

| Action | File | Notes |
|---|---|---|
| CREATE | `test/Ordering.Tests/Ordering.Tests.csproj` | New service test project with xUnit, Moq, MVC test host, and SQL Server Testcontainers package |
| CREATE | `test/Ordering.Tests/coverlet.runsettings` | Service-scoped coverage exclusions only |
| CREATE | `test/Ordering.Tests/README.md` | Standard test README with run commands and Docker notes |
| CREATE | `test/Ordering.Tests/run-with-docker.ps1` | Canonical local command for provider-backed slice |
| CREATE | `test/Ordering.Tests/Support/OrderingApiTestHost.cs` | Shared HTTP host/test-server bootstrap |
| CREATE | `test/Ordering.Tests/Support/OrderingSqlServerFixture.cs` | Testcontainers SQL Server lifecycle + seeded DB wiring |
| CREATE | `test/Ordering.Tests/Integration/OrderingSqlServerCollection.cs` | xUnit collection for serialized provider-backed tests |
| CREATE | `test/Ordering.Tests/Unit/Validators/CreateOrderCommandValidatorTests.cs` | Create validator coverage |
| CREATE | `test/Ordering.Tests/Unit/Validators/UpdateOrderCommandValidatorTests.cs` | Update validator coverage |
| CREATE | `test/Ordering.Tests/Unit/Validators/DeleteOrderCommandValidatorTests.cs` | Delete validator coverage |
| CREATE | `test/Ordering.Tests/Unit/Handlers/CreateOrderHandlerTests.cs` | Mapping + persistence call coverage |
| CREATE | `test/Ordering.Tests/Unit/Handlers/UpdateOrderHandlerTests.cs` | Success + not-found branches |
| CREATE | `test/Ordering.Tests/Unit/Handlers/DeleteOrderHandlerTests.cs` | Success + not-found branches |
| CREATE | `test/Ordering.Tests/Unit/Domain/OrderTests.cs` | Aggregate invariants and domain event checks |
| CREATE | `test/Ordering.Tests/Unit/EventHandlers/BasketCheckoutEventHandlerTests.cs` | Message-to-command mapping |
| CREATE | `test/Ordering.Tests/Unit/EventHandlers/OrderCreatedEventHandlerTests.cs` | Feature-flag branch and publish behavior |
| CREATE | `test/Ordering.Tests/Unit/EventHandlers/OrderUpdatedEventHandlerTests.cs` | Logging/completion branch |
| CREATE | `test/Ordering.Tests/Unit/Infrastructure/DispatchDomainEventsInterceptorTests.cs` | Publish + clear domain events behavior |
| CREATE | `test/Ordering.Tests/Unit/Queries/GetOrdersHandlerTests.cs` | Pagination/default shaping |
| CREATE | `test/Ordering.Tests/Unit/Queries/GetOrderByCustomerHandlerTests.cs` | Filter semantics |
| CREATE | `test/Ordering.Tests/Unit/Queries/GetOrdersByNameHandlerTests.cs` | Contains-filter semantics |
| CREATE | `test/Ordering.Tests/Integration/Endpoints/OrderingEndpointsTests.cs` | Mock-host HTTP contract tests |
| CREATE | `test/Ordering.Tests/Integration/Endpoints/OrderingEndpointsWithSqlServerTests.cs` | Provider-backed HTTP + persistence + health tests |
| CREATE | `test/Ordering.Tests/Routing/OrderingRoutingTests.cs` | Route registration and disambiguation |
| MODIFY | `src/eshop-microservices.slnx` | Add `../test/Ordering.Tests/Ordering.Tests.csproj` |

## 3. Phase Plan

### Phase 1: Scaffold the Ordering test project

Create the project and repository-level registration first so all later test files have a stable home.

**Files**
- `test/Ordering.Tests/Ordering.Tests.csproj`
- `src/eshop-microservices.slnx`
- `test/Ordering.Tests/coverlet.runsettings`

**Implementation details**
- Use the same baseline packages as Basket/Catalog tests:
  - `Microsoft.NET.Test.Sdk`
  - `xunit`
  - `xunit.runner.visualstudio`
  - `Moq`
  - `Microsoft.AspNetCore.Mvc.Testing`
  - `coverlet.collector`
- Add one provider package for the SQL Server slice.
  - Preferred: `Testcontainers.MsSql`
- Add a project reference to `src/Services/Ordering/Ordering.API/Ordering.API.csproj`.
- Add the test project to `src/eshop-microservices.slnx` beside the existing Basket/Catalog test projects.

**Coverage exclusions**
- `**/Program.cs`
- `**/obj/**`
- `**/Ordering.Infrastructure/Data/Migrations/**`
- `**/Ordering.Infrastructure/Extensions/InitialData.cs`
- `**/BuildingBlocks/BuildingBlocks/**`
- `**/BuildingBlocks/BuildingBlocks.Messaging/**`

**Learning note**: keep exclusions limited to bootstrap, seed, generated, and shared-library code so Ordering coverage remains an honest service KPI.

### Phase 2: Build the shared test harness and local runbook

Set up the reusable support layer before implementing individual tests so the suite uses one consistent boot path.

**Files**
- `test/Ordering.Tests/Support/OrderingApiTestHost.cs`
- `test/Ordering.Tests/Support/OrderingSqlServerFixture.cs`
- `test/Ordering.Tests/Integration/OrderingSqlServerCollection.cs`
- `test/Ordering.Tests/run-with-docker.ps1`
- `test/Ordering.Tests/README.md`

**Implementation details**
- `OrderingApiTestHost.cs`
  - Mirror the Basket/Catalog host pattern with `UseTestServer()`.
  - Provide:
    - `StartAsync(...)` for mock-based endpoint tests
    - `StartWithSqlServerAsync(string connectionString, IMessageBus bus, IPublishEndpoint publishEndpoint, IFeatureManager featureManager)` for provider-backed tests
    - `BuildAppForRouteInspection()` for routing tests
  - Register Ordering query handlers, validators, health checks, and endpoint mappings using the same production route surface.
- `OrderingSqlServerFixture.cs`
  - Start and dispose a SQL Server container.
  - Apply migrations or create schema through the actual `ApplicationDbContext`.
  - Seed deterministic orders/customers/products using Ordering domain models or reuse `InitialData` selectively.
  - Expose the container connection string to tests.
- `OrderingSqlServerCollection.cs`
  - Disable parallelization for provider-backed tests.
- `run-with-docker.ps1`
  - Follow the Catalog script shape.
  - Set `ORDERING_REQUIRE_DOCKER_TESTS=true` before running tests.
  - Use `dotnet test test/Ordering.Tests/Ordering.Tests.csproj --collect:"XPlat Code Coverage"` when `-Coverage` is enabled.
- `README.md`
  - Use the repository README structure:
    - `Current Status`
    - `Coverage Exclusions`
    - `Run Commands`
    - `Docker or External Setup`
    - `Remaining Gaps and Improvements`

**Docker policy**
- Default `dotnet test` run: provider-backed tests skip when Docker is unavailable.
- Docker-backed script: fail fast if Docker cannot be started or reached.

### Phase 3: Implement mock-solvable unit coverage

Cover validators, pure aggregate behavior, event handlers, interceptor behavior, and command/query decisions without a real SQL Server.

**Files**
- `test/Ordering.Tests/Unit/Validators/CreateOrderCommandValidatorTests.cs`
- `test/Ordering.Tests/Unit/Validators/UpdateOrderCommandValidatorTests.cs`
- `test/Ordering.Tests/Unit/Validators/DeleteOrderCommandValidatorTests.cs`
- `test/Ordering.Tests/Unit/Handlers/CreateOrderHandlerTests.cs`
- `test/Ordering.Tests/Unit/Handlers/UpdateOrderHandlerTests.cs`
- `test/Ordering.Tests/Unit/Handlers/DeleteOrderHandlerTests.cs`
- `test/Ordering.Tests/Unit/Domain/OrderTests.cs`
- `test/Ordering.Tests/Unit/EventHandlers/BasketCheckoutEventHandlerTests.cs`
- `test/Ordering.Tests/Unit/EventHandlers/OrderCreatedEventHandlerTests.cs`
- `test/Ordering.Tests/Unit/EventHandlers/OrderUpdatedEventHandlerTests.cs`
- `test/Ordering.Tests/Unit/Infrastructure/DispatchDomainEventsInterceptorTests.cs`
- `test/Ordering.Tests/Unit/Queries/GetOrdersHandlerTests.cs`
- `test/Ordering.Tests/Unit/Queries/GetOrderByCustomerHandlerTests.cs`
- `test/Ordering.Tests/Unit/Queries/GetOrdersByNameHandlerTests.cs`

**Priority cases**

#### Validators
- `CreateOrderCommandValidator`
  - valid command passes
  - empty `OrderName` fails
  - default/empty `CustomerId` fails based on current validator behavior
  - empty `OrderItems` fails
- `UpdateOrderCommandValidator`
  - empty `Id` fails
  - empty `OrderName` fails
  - default/empty `CustomerId` fails
- `DeleteOrderCommandValidator`
  - empty `OrderId` fails
  - valid `OrderId` passes

#### Aggregate / domain
- `Order.Create` raises one `OrderCreatedEvent`
- `Order.Update` raises one `OrderUpdatedEvent`
- `Order.Add` updates `TotalAmount` as `quantity * price` sum
- `Order.Add` throws for zero/negative quantity
- `Order.Add` throws for zero/negative price
- `Remove` deletes an existing item and ignores unknown item ids safely

#### Command handlers
- `CreateOrderHandler`
  - adds one order to the DbSet/context and saves once
  - maps addresses, payment, and items into the aggregate correctly
  - returns a non-empty order id
- `UpdateOrderHandler`
  - existing order returns `UpdateOrderResult(true)` and saves once
  - missing order returns `UpdateOrderNotFound` and does not save
- `DeleteOrderHandler`
  - existing order returns `DeleteOrderResult(true)` and removes the entity
  - missing order returns `DeleteOrderNotFound` and does not save

#### Query handlers
- `GetOrdersHandler`
  - preserves requested `PageIndex`/`PageSize`
  - returns deterministic ordering by `OrderName`
  - returns correct total count independent of page size
- `GetOrderByCustomerHandler`
  - filters only by `CustomerId`
  - returns empty list when no matches exist
- `GetOrdersByNameHandler`
  - performs substring matching
  - returns empty list when no matches exist

#### Eventing and infrastructure
- `BasketCheckoutEventHandler`
  - maps all checkout fields into `CreateOrderCommand`
  - maps each basket item into `OrderItemDto`
  - invokes `IMessageBus` exactly once
- `OrderCreatedEventHandler`
  - feature flag enabled publishes one integration event
  - feature flag disabled publishes nothing
- `OrderUpdatedEventHandler`
  - completes successfully without throwing
- `DispatchDomainEventsInterceptor`
  - publishes each pending domain event once
  - clears domain events after dispatch

**Implementation note**
- For handler/query unit tests, prefer a lightweight EF Core test context or mocked `IApplicationDbContext` only where the branch is simple.
- For query-shape assertions, use an EF Core in-memory/SQLite test context only as a mock-solvable helper, not as the authoritative provider-backed proof.

### Phase 4: Add routing and mock-host HTTP contract tests

Verify the minimal API surface, route disambiguation, and validation short-circuit behavior with a fast host harness.

**Files**
- `test/Ordering.Tests/Routing/OrderingRoutingTests.cs`
- `test/Ordering.Tests/Integration/Endpoints/OrderingEndpointsTests.cs`

**Routing cases**
- `/orders` group is registered
- route templates exist for:
  - `GET /orders`
  - `GET /orders/{orderName}`
  - `GET /orders/customer/{customerId}`
  - `POST /orders`
  - `PUT /orders`
  - `DELETE /orders/{id}`
  - `GET /health`
- endpoint names exist:
  - `GetOrders`
  - `GetOrdersByName`
  - `GetOrdersByCustomer`
  - `CreateOrder`
  - `UpdateOrder`
  - `DeleteOrder`
- `/orders/customer/{customerId}` is not captured by the name route

**Mock-host endpoint cases**
- `POST /orders`
  - valid request returns `201 Created` and location header
  - invalid request returns `400 Bad Request` and bus is not invoked
- `PUT /orders`
  - bus returns success -> `200 OK`
  - bus returns `UpdateOrderNotFound` -> `404 Not Found`
  - invalid request -> `400 Bad Request`
- `DELETE /orders/{id}`
  - bus returns success -> `200 OK`
  - bus returns `DeleteOrderNotFound` -> `404 Not Found`
  - malformed `Guid` -> `400 Bad Request`
- `GET /orders`
  - returns paginated payload and honors explicit pagination query params
  - returns default `PageIndex = 0`, `PageSize = 10` when omitted
- `GET /orders/{orderName}`
  - returns `200 OK` with matching orders
  - returns `200 OK` with empty array when no matches exist
- `GET /orders/customer/{customerId}`
  - returns `200 OK` with matching orders
  - returns `200 OK` with empty array when no matches exist

### Phase 5: Add provider-backed SQL Server integration tests

Implement the minimal real-provider slice required by the spec. Keep it small and intentional.

**Files**
- `test/Ordering.Tests/Integration/Endpoints/OrderingEndpointsWithSqlServerTests.cs`

**Required SQL Server-backed scenarios**
- `GET /orders`
  - seeded orders are returned in `OrderName` order
  - `Count` reflects total rows, not page size
  - pagination boundaries work as expected
- `GET /orders/{orderName}`
  - substring filter is translated correctly against SQL Server
- `GET /orders/customer/{customerId}`
  - customer filter returns only expected orders
- `POST /orders`
  - persists order, addresses, payment, order items, and computed total amount
  - `OrderCreatedEvent` is published through the real save pipeline
- `PUT /orders`
  - existing order is updated in the database
  - at least one end-to-end update path confirms domain event publication
- `DELETE /orders/{id}`
  - row is removed from SQL Server
- `GET /health`
  - returns healthy when SQL Server container is reachable

**Provider-backed assertions**
- Verify persisted order data through `ApplicationDbContext` after HTTP calls.
- Capture bus publication or downstream observable effect to prove end-to-end domain event dispatch for at least one create/update scenario.
- Keep transport collaborators mocked unless the test specifically needs them.

**Not included in the provider slice**
- Full RabbitMQ/MassTransit transport integration
- Feature-management infrastructure integration beyond deterministic stubbed flag values

### Phase 6: Final documentation and validation

Complete the service README and validate the suite with the two canonical commands.

**Files**
- `test/Ordering.Tests/README.md`

**README content to finalize after first passing run**
- latest validated date
- canonical local command
- docker-backed coverage command
- current pass count
- latest coverage report path
- remaining known gaps, if any

## 4. Test Class Layout

```text
test/Ordering.Tests/
  Ordering.Tests.csproj
  coverlet.runsettings
  README.md
  run-with-docker.ps1
  Support/
    OrderingApiTestHost.cs
    OrderingSqlServerFixture.cs
  Integration/
    OrderingSqlServerCollection.cs
    Endpoints/
      OrderingEndpointsTests.cs
      OrderingEndpointsWithSqlServerTests.cs
  Routing/
    OrderingRoutingTests.cs
  Unit/
    Domain/
      OrderTests.cs
    EventHandlers/
      BasketCheckoutEventHandlerTests.cs
      OrderCreatedEventHandlerTests.cs
      OrderUpdatedEventHandlerTests.cs
    Handlers/
      CreateOrderHandlerTests.cs
      UpdateOrderHandlerTests.cs
      DeleteOrderHandlerTests.cs
    Infrastructure/
      DispatchDomainEventsInterceptorTests.cs
    Queries/
      GetOrdersHandlerTests.cs
      GetOrderByCustomerHandlerTests.cs
      GetOrdersByNameHandlerTests.cs
    Validators/
      CreateOrderCommandValidatorTests.cs
      UpdateOrderCommandValidatorTests.cs
      DeleteOrderCommandValidatorTests.cs
```

## 5. Dependencies and Mocking Strategy

| Dependency | Test Layer | Strategy |
|---|---|---|
| `IMessageBus` | Unit, routing, mock-host integration | Moq; return typed command results and capture `PublishAsync`/`InvokeAsync` calls |
| `IPublishEndpoint` | Unit | Moq for `OrderCreatedEventHandler` publish assertions |
| `IFeatureManager` | Unit, provider-backed integration | Moq or deterministic stub for `OrderFullfilment` branches |
| `ILogger<T>` | Unit | Mock only when call verification matters; otherwise use `Mock.Of<ILogger<T>>()` |
| `IApplicationDbContext` | Unit | Lightweight context double for simple branch tests |
| `ApplicationDbContext` + SQL Server | Provider-backed integration | Testcontainers-backed real database |

## 6. Execution and Verification

### Canonical Commands

```powershell
dotnet test test/Ordering.Tests/Ordering.Tests.csproj
```

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Ordering.Tests/run-with-docker.ps1 -Coverage
```

### Exit Criteria

- All planned unit, routing, and integration tests are implemented and passing.
- Service-owned line coverage is at least 90%.
- Critical branch coverage is at least 70%.
- All six `/orders` routes and `/health` are covered by routing or integration assertions.
- Invalid create/update/delete requests prove dispatch suppression through `400 Bad Request` behavior.
- At least one create or update provider-backed scenario proves domain event publication through the real save pipeline.

### Grep Checks

After implementation, verify these structure expectations:
- `test/Ordering.Tests/**` contains `Unit/`, `Integration/`, `Routing/`, and `Support/`
- `src/eshop-microservices.slnx` contains `../test/Ordering.Tests/Ordering.Tests.csproj`
- `test/Ordering.Tests/README.md` contains `Current Status`, `Coverage Exclusions`, `Run Commands`, `Docker or External Setup`, and `Remaining Gaps and Improvements`

## 7. Coverage Gap Checklist

- [x] List excluded files and justification
- [x] Identify non-service assemblies and keep them out of Ordering coverage KPI
- [x] Separate mock-solvable vs provider-dependent cases
- [x] Include malformed route values and default pagination behavior
- [x] Define a Docker-backed local command for the provider slice
- [x] Define expected behavior when Docker is unavailable locally

## 8. Gaps and Assumptions

- Gap: No Ordering test project exists yet.
  - Why it matters: all phases in this plan begin with project scaffolding, so no partial implementation can be validated until the project exists.
  - Resolution: complete Phase 1 before writing any test classes.

- Gap: The repository does not yet show an existing SQL Server Testcontainers pattern.
  - Why it matters: package choice and fixture code need one repo-consistent implementation.
  - Resolution: standardize on `Testcontainers.MsSql` in the new test project and keep the provider-backed fixture isolated to `Support/OrderingSqlServerFixture.cs`.

- Assumption: `InitialData` or equivalent deterministic seed data can be used safely for read-path integration tests.
  - Why it matters: seeded orders simplify pagination and filtering assertions.
  - Resolution: if `InitialData` proves too coupled to runtime bootstrap, seed directly inside the fixture with explicit order names and customer ids.

- Assumption: HTTP host tests can use the endpoint mapping layer directly with test-only dependency registration instead of booting the full production `Program` pipeline.
  - Why it matters: this matches current Basket/Catalog tests and keeps the fast suite deterministic.
  - Resolution: only escalate to full `WebApplicationFactory<Program>` if a route or middleware behavior cannot be reproduced with `UseTestServer()`.