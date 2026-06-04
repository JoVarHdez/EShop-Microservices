# Shopping.Web.Razor — .NET 10 Real-World Modernization

## 1. Feature Summary

The `Shopping.Web.Razor` project was originally written following a .NET 8 course structure and later retargeted to .NET 10. While it runs correctly, it carries several course-level patterns that are not representative of production .NET 10 applications: raw configuration reads instead of the Options pattern, Refit contracts polluted with business logic, three identical and repetitive HTTP client registrations, client-side data filtering that should be delegated to the backend, and no HTTP resilience whatsoever. This modernization replaces those patterns with idiomatic .NET 10 practices — typed options, a clean Refit/service-layer split, a consolidated HTTP client factory setup, backend-driven category filtering, and standardized resilience pipelines — producing a codebase that serves as a real-world Razor Pages reference, not a course scaffolding.

## 2. Data Model / Entities

### ApiSettings (new typed options class)
- GatewayAddress: the base URL for the YARP API gateway

### IBasketApiClient (rename/refactor of IBasketService Refit contract)
- GetBasketAsync(userName): GET Refit contract only
- StoreBasketAsync(request): POST Refit contract only
- DeleteBasketAsync(userName): DELETE Refit contract only
- CheckoutBasketAsync(request): POST Refit contract only

### IBasketService (new business-logic interface)
- LoadUserBasketAsync(): resolves the current user and loads or seeds an empty cart

### BasketService (new concrete class)
- Wraps IBasketApiClient
- Contains LoadUserBasketAsync() moved from the old default interface method
- Hardcoded username "swn" remains (auth is out of scope), extracted as a named constant

### ICatalogService (Refit contract — unchanged shape)
- GetProductsAsync(pageNumber, pageSize)
- GetProductAsync(id)
- GetProductsByCategoryAsync(category): already declared but currently bypassed

### IOrderingService (Refit contract — unchanged shape)
- GetOrdersAsync, GetOrdersByNameAsync, GetOrdersByCustomerAsync

### ProductListModel (page model — modified)
- Delegates category-filtered requests to ICatalogService.GetProductsByCategoryAsync instead of fetching all products and filtering in memory

## 3. Business Rules & Constraints

The system MUST enforce the following non-negotiable rules:

1. The gateway base URL MUST be read through `IOptions<ApiSettings>` — direct access to `IConfiguration["ApiSettings:GatewayAddress"]` is forbidden after this change.
2. Refit interfaces MUST be pure HTTP contracts. No default interface methods or business logic are permitted inside them.
3. Business logic (e.g., "load basket or return empty cart on failure") MUST live in a service class, not on an interface.
4. Every Refit client (catalog, basket, ordering) MUST be registered through a shared helper or extension method — no copy-pasted `AddRefitClient` blocks.
5. Every Refit client MUST have a resilience pipeline (standard retry + circuit-breaker) applied via `Microsoft.Extensions.Http.Resilience`.
6. Category filtering for the product list MUST be performed by calling the dedicated backend endpoint (`/catalog-service/products/category/{category}`), not by fetching all products and filtering in memory.
7. User identity (userName, customerId) remains hardcoded as named constants — no authentication changes are in scope.

## 4. Acceptance Criteria

The feature is complete when:

- [ ] An `ApiSettings` class exists and is registered via `builder.Services.Configure<ApiSettings>(...)`.
- [ ] `Program.cs` reads the gateway address exclusively through `IOptions<ApiSettings>` (or `ApiSettings` bound via `Configure`), with zero `builder.Configuration["..."]` calls remaining.
- [ ] `IBasketService` Refit interface contains only four Refit-attributed methods and no default interface implementation.
- [ ] A concrete `BasketService` class implements a new `IBasketService` business interface exposing `LoadUserBasketAsync()`.
- [ ] All page models (`IndexModel`, `CartModel`, `CheckoutModel`) inject `IBasketService` (the business interface) and call `LoadUserBasketAsync()` successfully.
- [ ] A single extension method or helper centralizes Refit client registration; `Program.cs` does not contain three separate `AddRefitClient` blocks.
- [ ] All three Refit clients have `AddStandardResilienceHandler()` (or equivalent from `Microsoft.Extensions.Http.Resilience`) applied.
- [ ] `ProductListModel.OnGetAsync` calls `ICatalogService.GetProductsByCategoryAsync(categoryName)` when a category is selected, eliminating the in-memory LINQ filter.
- [ ] The application builds with zero errors and zero new compiler warnings.
- [ ] All existing pages (Index, ProductList, ProductDetail, Cart, Checkout, Confirmation, OrderList) render correctly when run against the gateway.

## 5. Out of Scope

The following are explicitly NOT part of this feature:

- ASP.NET Core Identity, cookie authentication, or any real user authentication/authorization implementation.
- Replacing the hardcoded `customerId` GUID with a real claims-based identity.
- Migrating from Razor Pages to Blazor or any other front-end framework.
- Changing the visual design, CSS, or Razor view markup.
- Adding health check endpoints or OpenTelemetry instrumentation.
- Modifying any backend services (Catalog, Basket, Ordering, YARP gateway).
- Pagination improvements on the product list or order list pages.
- Unit or integration test projects.

## 6. Open Questions

All decisions resolved.

1. **Where should the Refit client registration helper live?**
   - Context: The helper that calls `AddRefitClient<T>().AddStandardResilienceHandler()` for all three clients needs a home. Options affect discoverability and future extensibility.
   - **Decision: A) Static extension method on `IServiceCollection` inside a `ServiceExtensions.cs` file in the project root.**

2. **Should `BasketService` be registered as `Scoped` or `Transient`?**
   - Context: The service wraps an `HttpClient`-backed Refit client. Scoped is the convention for request-scoped services in Razor Pages; Transient is safe but less conventional.
   - **Decision: A) `AddScoped<IBasketService, BasketService>()` — recommended convention for Razor Pages.**

3. **Should `ApiSettings` validation be enforced at startup?**
   - Context: If `GatewayAddress` is missing or empty, the app will fail silently at runtime on the first HTTP call. Eager validation (e.g., `ValidateOnStart()`) surfaces the error immediately when the app boots.
   - **Decision: A) `builder.Services.AddOptions<ApiSettings>().BindConfiguration("ApiSettings").ValidateDataAnnotations().ValidateOnStart()` — fails fast.**
