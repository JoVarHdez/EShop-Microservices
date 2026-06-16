# Implementation Plan: Cross-Service Contract Consolidation (Microservices Preserved)

> Spec: [docs/specs/20260612-cross-service-contract-consolidation.md](../specs/20260612-cross-service-contract-consolidation.md)

**TL;DR** - Keep the current microservices topology, but replace weak integration contracts and placeholder mappings with consistent, ProductId-driven contracts. The plan focuses on four outcomes: ProductId-based Catalog-Discount alignment, real checkout item propagation from Basket to Ordering, consistent hardcoded development identity across Shopping/Basket/Ordering, and explicit deferral of gateway auth enforcement.

**Decisions applied from the spec**
- Keep `ProductName` as an optional compatibility field for one release window.
- Keep hardcoded identity for now, but centralize it so values are consistent.
- Use in-place contract update (no v2 contract split).
- Keep gateway auth gap visible but deferred in this phase.

---

## Relevant Files

| Action | File |
|--------|------|
| MODIFY | `src/BuildingBlocks/BuildingBlocks.Messaging/Events/BasketCheckoutEvent.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Protos/discount.proto` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Services/DiscountService.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Services/BasketService.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/Checkout.cshtml.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Pages/OrderList.cshtml.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Models/DevUserContext.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Services/IDevUserContextProvider.cs` |
| CREATE | `src/WebApps/Shopping.Web.Razor/Services/DevUserContextProvider.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/Program.cs` |
| MODIFY | `src/WebApps/Shopping.Web.Razor/appsettings.json` |
| MODIFY | `docs/PendingFeatures.md` |

Notes:
- No host merge and no service deletions are part of this plan.
- YARP runtime routing remains in place for now; only auth enforcement work is deferred.

---

## Phase 1 - Introduce shared contracts and remove placeholder order mapping risk

Start with shared contract corrections, because this unblocks all downstream behavior and has the lowest coupling risk.

### 1.1 - Extend checkout integration event with line items

**File**: `src/BuildingBlocks/BuildingBlocks.Messaging/Events/BasketCheckoutEvent.cs`

- **ADD** `BasketCheckoutLineItem` contract type with `ProductId`, `ProductName`, `Quantity`, and `UnitPrice`.
- **ADD** `Items` collection to `BasketCheckoutEvent`.
- **KEEP** existing address/payment fields unchanged.

```csharp
public record BasketCheckoutLineItem(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record BasketCheckoutEvent : IntegrationEvent
{
    public string UserName { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<BasketCheckoutLineItem> Items { get; set; } = [];
    // existing address/payment fields remain
}
```

> Learning note: the missing `Items` payload is the root cause behind hardcoded order items in Ordering.

### 1.2 - Map real basket lines into checkout event

**File**: `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs`

- **MODIFY** event construction to map line items from the retrieved basket into `eventMessage.Items`.
- **KEEP** publish-then-delete flow and existing success/failure semantics.
- **REMOVE** any assumption that `TotalPrice` alone is enough for downstream order creation.

- **UNCHANGED**: message broker usage (`IPublishEndpoint`) and endpoint contract shape.

### 1.3 - Replace placeholder order items with event-driven items

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs`

- **REMOVE** hardcoded `OrderItemDto` entries with fixed GUIDs.
- **ADD** mapping from `message.Items` into `OrderItemDto` values.
- **KEEP** the existing `CreateOrderCommand` dispatch path via Wolverine `IMessageBus`.

```csharp
OrderItems: [.. message.Items.Select(i => new OrderItemDto(orderId, i.ProductId, i.Quantity, i.UnitPrice))]
```

- **UNCHANGED**: address/payment mapping and order status initialization.

---

## Phase 2 - Consolidate Catalog-Discount identity contract (ProductId-first)

After event correctness is fixed, align discount lookup around ProductId while preserving temporary compatibility.

### 2.1 - Update discount gRPC contract for ProductId-based lookup

**File**: `src/Services/Discount/Discount.Grpc/Protos/discount.proto`

- **MODIFY** `GetDiscountRequest` to carry `productId` as required lookup key.
- **KEEP** `productName` as optional compatibility field for one release window.
- **REGENERATE** generated gRPC client/server code via normal build pipeline.

```proto
message GetDiscountRequest {
  string productId = 1;
  string productName = 2; // optional compatibility
}
```

> Learning note: this keeps migration risk low while establishing ProductId as authoritative.

### 2.2 - Implement ProductId lookup semantics in Discount service

**File**: `src/Services/Discount/Discount.Grpc/Services/DiscountService.cs`

- **ADD/MODIFY** lookup path to resolve by ProductId.
- **OPTIONALLY FALL BACK** to ProductName during transition window only.
- **KEEP** existing not-found fallback behavior (`Amount = 0`, descriptive message) unless business decision changes it.

- **UNCHANGED**: create/update/delete endpoints can remain as-is in this phase if they do not block lookup correctness.

### 2.3 - Call discount service with ProductId from Basket

**File**: `src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs`

- **MODIFY** `GetDiscountAsync` request mapping to include ProductId.
- **KEEP** current discount deduction loop behavior.
- **KEEP** ProductName in request only as compatibility during the transition window.

- **UNCHANGED**: basket persistence flow and validator behavior.

---

## Phase 3 - Enforce consistent hardcoded development identity (auth still deferred)

This phase implements your chosen direction: keep hardcoded values for now, but centralize them for consistency.

### 3.1 - Create shared development user context model and provider

**Files**:
- `src/WebApps/Shopping.Web.Razor/Models/DevUserContext.cs` (CREATE)
- `src/WebApps/Shopping.Web.Razor/Services/IDevUserContextProvider.cs` (CREATE)
- `src/WebApps/Shopping.Web.Razor/Services/DevUserContextProvider.cs` (CREATE)
- `src/WebApps/Shopping.Web.Razor/appsettings.json` (MODIFY)
- `src/WebApps/Shopping.Web.Razor/Program.cs` (MODIFY)

- **ADD** a simple provider abstraction that returns one hardcoded-but-centralized `UserName` and `CustomerId`.
- **ADD** configuration section for development identity defaults.
- **REGISTER** the provider in DI.

```csharp
public record DevUserContext(string UserName, Guid CustomerId);
```

- **UNCHANGED**: no authentication middleware is added.

### 3.2 - Replace scattered hardcoded identity usage in Shopping.Web.Razor

**Files**:
- `src/WebApps/Shopping.Web.Razor/Services/BasketService.cs`
- `src/WebApps/Shopping.Web.Razor/Pages/Checkout.cshtml.cs`
- `src/WebApps/Shopping.Web.Razor/Pages/OrderList.cshtml.cs`

- **REMOVE** direct literals (`"swn"`, `"test"`, duplicated fixed GUID) from these files.
- **USE** the shared provider for basket load, checkout payload identity fields, and order-by-customer queries.
- **KEEP** all page and service flows unchanged beyond identity source replacement.

> Learning note: this gives consistent behavior now and a clean seam to replace with IAM claims later.

---

## Phase 4 - Document deferred gateway auth scope explicitly

No gateway auth implementation is done here, but deferral should be explicit to avoid confusion.

### 4.1 - Update pending-features wording to reflect deferred auth in this contract phase

**File**: `docs/PendingFeatures.md`

- **MODIFY** Priority 1 text to tag gateway auth and identity-forwarding baseline as deferred for this phase.
- **KEEP** gap visible in backlog.
- **KEEP** Priority 0 IAM content unchanged.

- **UNCHANGED**: `src/APIGateways/YarpApiGateway/Program.cs` and gateway route config are not modified in this plan.

---

## Verification

1. **Build check**
   - `dotnet build src/eshop-microservices.slnx` succeeds with 0 errors.
   - If the known Discount reference lock appears in this workspace, use fallback:
     - `dotnet build src/eshop-microservices.slnx /p:ProduceReferenceAssembly=false`

2. **Grep checks** (must return no relevant matches in source files)
   - `new OrderItemDto(orderId, new Guid("00000000-0000-0000-0000-000000000001")`
   - `new OrderItemDto(orderId, new Guid("00000000-0000-0000-0000-000000000002")`
   - `private const string DefaultUserName = "swn"`
   - `Order.UserName = "test"`
   - `GetDiscountRequest { ProductName = product.ProductName }` *(without ProductId in the request)*

3. **Functional smoke tests**
   - Basket pricing path:
     - `POST /basket-service/basket` with items containing ProductId -> discount lookup resolves and basket prices are updated.
   - Checkout path:
     - `POST /basket-service/basket/checkout` for an existing basket -> publishes `BasketCheckoutEvent` with `Items`, clears basket, returns success.
   - Ordering path:
     - Consume checkout event -> created order contains item lines matching checkout item payload (no placeholders).
   - Shopping consistency path:
     - Basket load, checkout, and order list use the same development user identity source and return coherent data.

4. **Scope guard checks**
   - No new auth middleware appears in Shopping, Basket, Ordering, or Gateway for this phase.
   - Service boundaries remain intact (no host merger, no service deletions).
