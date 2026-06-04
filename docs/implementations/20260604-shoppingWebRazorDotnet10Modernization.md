# Implementation Plan: Shopping.Web.Razor — .NET 10 Real-World Modernization

> Spec: [`docs/specs/20260604-shoppingWebRazorDotnet10Modernization.md`](../specs/20260604-shoppingWebRazorDotnet10Modernization.md)

**TL;DR** — Add the resilience NuGet package first (Phase 1); then in parallel create the `ApiSettings` typed-options class + `ServiceExtensions` registration helper (Phase 2) and restructure the basket service layer — renaming the Refit contract to `IBasketApiClient` and introducing a new `BasketService` concrete class (Phase 3); then update all five page models to call the new business interface and to delegate category filtering to the backend (Phase 4); and finally rewire `Program.cs` to use the Options pattern and the shared extension method (Phase 5). Phases 2 and 3 are independent and can run in parallel.

---

## Relevant Files

| Action | File |
|--------|------|
| MODIFY | `src/WebApps/Shopping.Web.Razor/Shopping.Web.Razor.csproj` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Models/ApiSettings.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/ServiceExtensions.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Services/IBasketService.cs` → renamed to `IBasketApiClient.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Services/IBasketService.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Services/BasketService.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/ProductList.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/Index.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/Cart.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/Checkout.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/ProductDetail.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Program.cs` |

---

## Phase 1 — Add NuGet Package *(no dependencies — run immediately)*

Adding the resilience package first keeps the project in a compilable state throughout all subsequent phases.

### 1.1 — Add `Microsoft.Extensions.Http.Resilience`

`AddStandardResilienceHandler()` lives in this package and is not available in `Refit.HttpClientFactory` alone. Adding it before any code changes prevents mid-refactor build breaks.

**File**: `src/WebApps/Shopping.Web.Razor/Shopping.Web.Razor.csproj`

- Add inside the existing `<ItemGroup>`:
  ```xml
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.6.0" />
  ```
- **Unchanged**: all other package references and property group entries.

---

## Phase 2 — Typed Options & Registration Helper *(depends on Phase 1; parallel with Phase 3)*

Creating the `ApiSettings` class and `ServiceExtensions` in isolation avoids touching `Program.cs` until the new types and registrations are fully ready.

### 2.1 — Create `ApiSettings` typed-options class

The Options pattern (`IOptions<T>`) replaces raw `IConfiguration["ApiSettings:GatewayAddress"]` reads. Adding `[Required]` allows `.ValidateDataAnnotations().ValidateOnStart()` to surface a missing value at boot time rather than on the first HTTP call.

**File**: `src/WebApps/Shopping.Web.Razor/Models/ApiSettings.cs`  
*(new file — full content)*

```csharp
using System.ComponentModel.DataAnnotations;

namespace Shopping.Web.Razor.Models;

public class ApiSettings
{
    [Required]
    public string GatewayAddress { get; set; } = default!;
}
```

### 2.2 — Create `ServiceExtensions` registration helper

A single `AddApiClients()` extension method replaces the three copy-pasted `AddRefitClient` blocks in `Program.cs`. Using the `(IServiceProvider, HttpClient)` overload of `ConfigureHttpClient` resolves `IOptions<ApiSettings>` lazily from the built container, which is the idiomatic .NET 10 way to read typed options during client factory setup.

**File**: `src/WebApps/Shopping.Web.Razor/ServiceExtensions.cs`  
*(new file — full content)*

```csharp
using Microsoft.Extensions.Options;
using Refit;
using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Services;

namespace Shopping.Web.Razor;

public static class ServiceExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        static void ConfigureClient(IServiceProvider sp, HttpClient client)
        {
            var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.GatewayAddress);
        }

        services.AddRefitClient<ICatalogService>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddRefitClient<IBasketApiClient>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddRefitClient<IOrderingService>()
            .ConfigureHttpClient(ConfigureClient)
            .AddStandardResilienceHandler();

        services.AddScoped<IBasketService, BasketService>();

        return services;
    }
}
```

> **Learning note**: The `ConfigureClient` local function is defined once and reused for all three clients — this is how .NET 10 avoids repeating base-address wiring without introducing a separate helper class.

---

## Phase 3 — Basket Service Layer Restructure *(depends on Phase 1; parallel with Phase 2)*

Splitting the Refit HTTP contract from the business-logic service is the core architectural change. The three steps in this phase are independent of each other and can be done in any order, but they all need to be complete before Phase 4.

### 3.1 — Rename `IBasketService` Refit contract to `IBasketApiClient`

The existing file holds both the Refit HTTP contract and a `LoadUserBasket()` default interface method that contains business logic. Renaming the interface and file isolates the pure HTTP contract, making `IBasketService` name available for the new business-logic interface in Step 3.2.

**File**: `src/WebApps/Shopping.Web.Razor/Services/IBasketService.cs`

- Rename the **file** from `IBasketService.cs` to `IBasketApiClient.cs`.
- Rename the **interface** from `IBasketService` to `IBasketApiClient`.
- Remove the entire `LoadUserBasket()` default method (lines from `public async Task<ShoppingCartModel> LoadUserBasket()` through its closing brace).
- **Unchanged**: all four `[Get]`/`[Post]`/`[Delete]` Refit method declarations, both `using` statements.

After the edit the file should read:

```csharp
using Refit;
using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public interface IBasketApiClient
{
    [Get("/basket-service/basket/{userName}")]
    Task<GetBasketResponse> GetBasketAsync(string userName);

    [Post("/basket-service/basket")]
    Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request);

    [Delete("/basket-service/basket/{userName}")]
    Task<DeleteBasketResponse> DeleteBasketAsync(string userName);

    [Post("/basket-service/basket/checkout")]
    Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request);
}
```

### 3.2 — Create `IBasketService` business-logic interface

This new interface is what page models inject. It exposes the same basket-mutation operations that page models need, plus `LoadUserBasketAsync()`. Page models never need to know about `IBasketApiClient`.

**File**: `src/WebApps/Shopping.Web.Razor/Services/IBasketService.cs`  
*(new file — full content)*

```csharp
using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public interface IBasketService
{
    Task<ShoppingCartModel> LoadUserBasketAsync();
    Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request);
    Task<DeleteBasketResponse> DeleteBasketAsync(string userName);
    Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request);
}
```

### 3.3 — Create `BasketService` concrete class

`BasketService` wraps `IBasketApiClient` and adds `LoadUserBasketAsync()`, which is the only piece of logic that was previously polluting the Refit interface. The hardcoded username is extracted as a `private const` so it can be found and replaced in a future auth modernization.

**File**: `src/WebApps/Shopping.Web.Razor/Services/BasketService.cs`  
*(new file — full content)*

```csharp
using Shopping.Web.Razor.Models.Basket;

namespace Shopping.Web.Razor.Services;

public class BasketService(IBasketApiClient basketApiClient) : IBasketService
{
    private const string DefaultUserName = "swn";

    public async Task<ShoppingCartModel> LoadUserBasketAsync()
    {
        try
        {
            var response = await basketApiClient.GetBasketAsync(DefaultUserName);
            return response.Cart;
        }
        catch (Exception)
        {
            return new ShoppingCartModel
            {
                UserName = DefaultUserName,
                Items = [],
            };
        }
    }

    public Task<StoreBasketResponse> StoreBasketAsync(StoreBasketRequest request)
        => basketApiClient.StoreBasketAsync(request);

    public Task<DeleteBasketResponse> DeleteBasketAsync(string userName)
        => basketApiClient.DeleteBasketAsync(userName);

    public Task<CheckoutBasketResponse> CheckoutBasketAsync(CheckoutBasketRequest request)
        => basketApiClient.CheckoutBasketAsync(request);
}
```

> **Learning note**: Thin delegation methods (`=> basketApiClient.Method(arg)`) keep the wrapper lean. Only `LoadUserBasketAsync` adds real business logic (the try/catch fallback to an empty cart).

---

## Phase 4 — Update Page Models *(depends on Phase 3)*

All five page models need the `LoadUserBasket()` → `LoadUserBasketAsync()` rename. `ProductListModel` also needs its in-memory category filter replaced with a backend call. All steps in this phase are **independent and can run in parallel**.

### 4.1 — Update `ProductListModel`

Two changes: rename the basket method call, and replace the in-memory LINQ filter with `GetProductsByCategoryAsync()`. The backend endpoint already exists and is declared on `ICatalogService`; it was never used from this page model.

**File**: `src/WebApps/Shopping.Web.Razor/Pages/ProductList.cshtml.cs`

- In `OnGetAsync`, replace the in-memory filter block:
  ```csharp
  // REMOVE this entire block:
  var response = await catalogService.GetProductsAsync();

  CategoryList = response.Products.SelectMany(p => p.Category).Distinct();

  if (!string.IsNullOrWhiteSpace(categoryName))
  {
      ProductList = response.Products.Where(p => p.Category.Contains(categoryName));
      SelectedCategory = categoryName;
  }
  else
  {
      ProductList = response.Products;
  }
  ```
  With:
  ```csharp
  if (!string.IsNullOrWhiteSpace(categoryName))
  {
      var categoryResponse = await catalogService.GetProductsByCategoryAsync(categoryName);
      ProductList = categoryResponse.Products;
      SelectedCategory = categoryName;

      var allResponse = await catalogService.GetProductsAsync();
      CategoryList = allResponse.Products.SelectMany(p => p.Category).Distinct();
  }
  else
  {
      var response = await catalogService.GetProductsAsync();
      CategoryList = response.Products.SelectMany(p => p.Category).Distinct();
      ProductList = response.Products;
  }
  ```
- In `OnPostAddToCartAsync`, rename `basketService.LoadUserBasket()` → `basketService.LoadUserBasketAsync()`.
- **Unchanged**: `OnPostAddToCartAsync` logic (add item, store basket, redirect), all constructor parameters, all `[BindProperty]` declarations.

### 4.2 — Update `IndexModel`

**File**: `src/WebApps/Shopping.Web.Razor/Pages/Index.cshtml.cs`

- In `OnPostAddToCartAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- **Unchanged**: `OnGetAsync`, null-check on `product`, cart item construction, redirect to Cart page.

### 4.3 — Update `CartModel`

**File**: `src/WebApps/Shopping.Web.Razor/Pages/Cart.cshtml.cs`

- In `OnGetAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- In `OnPostRemoveToCartAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- **Unchanged**: `RemoveAll` predicate, `StoreBasketAsync` call, redirect.

### 4.4 — Update `CheckoutModel`

**File**: `src/WebApps/Shopping.Web.Razor/Pages/Checkout.cshtml.cs`

- In `OnGetAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- In `OnPostCheckOutAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- **Unchanged**: `ModelState.IsValid` guard, hardcoded `CustomerId` and `UserName` stubs, `TotalPrice` assignment, `CheckoutBasketAsync` call, redirect to Confirmation.

### 4.5 — Update `ProductDetailModel`

**File**: `src/WebApps/Shopping.Web.Razor/Pages/ProductDetail.cshtml.cs`

- In `OnPostAddToCartAsync`, rename `await basketService.LoadUserBasket()` → `await basketService.LoadUserBasketAsync()`.
- **Unchanged**: `OnGetAsync`, null-check on `Product`, cart item construction with `Color` and `Quantity` from bound properties, `StoreBasketAsync` call, redirect.

---

## Phase 5 — Rewire `Program.cs` *(depends on Phases 2 and 3)*

`Program.cs` is updated last because it references `ApiSettings` (Phase 2) and `IBasketApiClient` is registered inside `ServiceExtensions` (Phase 2), which depends on the type existing (Phase 3).

### 5.1 — Replace raw config reads and repeated client registrations

**File**: `src/WebApps/Shopping.Web.Razor/Program.cs`

- Add at the top: `using Shopping.Web.Razor;`
- Replace the three `AddRefitClient` blocks and add the Options registration. The existing code:
  ```csharp
  builder.Services.AddRefitClient<ICatalogService>()
      .ConfigureHttpClient( c =>
      {
          c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
      });

  builder.Services.AddRefitClient<IBasketService>()
      .ConfigureHttpClient( c =>
      {
          c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
      });

  builder.Services.AddRefitClient<IOrderingService>()
      .ConfigureHttpClient( c =>
      {
          c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
      });
  ```
  Becomes:
  ```csharp
  builder.Services.AddOptions<ApiSettings>()
      .BindConfiguration("ApiSettings")
      .ValidateDataAnnotations()
      .ValidateOnStart();

  builder.Services.AddApiClients();
  ```
- Add `using Shopping.Web.Razor.Models;` if not already pulled in by implicit usings (needed for `ApiSettings`).
- **Unchanged**: `AddRazorPages()`, all middleware (`UseExceptionHandler`, `UseHsts`, `UseHttpsRedirection`, `UseRouting`, `UseAuthorization`), `MapStaticAssets()`, `MapRazorPages().WithStaticAssets()`, `await app.RunAsync()`.

---

## Verification

1. **Build**: `dotnet build src/eshop-microservices.slnx` must succeed with **0 errors and 0 warnings**.

2. **Grep checks** — run from `src/WebApps/Shopping.Web.Razor/`:

   | Symbol / pattern | Expected result |
   |---|---|
   | `IConfiguration\["ApiSettings` | **0 matches** — raw config key access is gone |
   | `LoadUserBasket\(\)` (without `Async`) | **0 matches** — old sync-named method is gone |
   | `AddRefitClient` in `Program.cs` | **0 matches** — all registrations moved to `ServiceExtensions` |
   | `public async Task.*LoadUserBasket` in `IBasketService.cs` | **0 matches** — default method removed from Refit contract |

3. **Startup validation smoke test**: stop the gateway, start only `Shopping.Web.Razor` with `ApiSettings:GatewayAddress` removed from `appsettings.json` → the app MUST throw a `OptionsValidationException` on boot (not on first request).

4. **HTTP smoke tests** (run with full `docker-compose up`):

   | Request | Scenario | Expected |
   |---|---|---|
   | `GET /` | Normal load | `200 OK`, product grid rendered |
   | `GET /ProductList` | No category | `200 OK`, all products listed |
   | `GET /ProductList?categoryName=Smart Phone` | Valid category | `200 OK`, filtered products only |
   | `GET /Cart` | Existing basket for user `swn` | `200 OK`, cart items displayed |
   | `GET /Cart` | Basket service unreachable | `200 OK`, empty cart rendered (fallback in `BasketService`) |
   | `POST /Cart?handler=RemoveToCart` with valid `productId` | Item in cart | Redirects back to Cart with item removed |
   | `GET /Checkout` | Cart has items | `200 OK`, checkout form rendered |
   | `POST /Checkout?handler=CheckOut` with valid form | All fields filled | Redirects to `/Confirmation` |
   | `GET /OrderList` | Orders exist for hardcoded customer | `200 OK`, order rows displayed |
