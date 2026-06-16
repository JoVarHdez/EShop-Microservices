# Routing Test Matrix

Test scenarios for API endpoints, message handlers, and YARP gateway routing in eShop microservices.

## API Endpoint Routing

### Basket Service

| Endpoint | Method | Route | Handler | Success Scenario | Error Scenario |
|----------|--------|-------|---------|------------------|----------------|
| Get Basket | GET | `/api/v1/basket/{id}` | `GetBasketById` | Return 200 + basket JSON | 404 if not found |
| Add Item | POST | `/api/v1/basket/{id}/items` | `AddBasketItem` | Return 201 + updated basket | 400 if quantity invalid |
| Update Item | PUT | `/api/v1/basket/{id}/items/{itemId}` | `UpdateBasketItem` | Return 200 + updated basket | 404 if item not found |
| Checkout | POST | `/api/v1/basket/{id}/checkout` | `CheckoutBasket` | Return 202 + event published | 409 if basket empty |
| Delete Basket | DELETE | `/api/v1/basket/{id}` | `DeleteBasket` | Return 204 No Content | 404 if not found |

#### Test Cases: Basket Service Endpoints

```csharp
[Fact]
public async Task GetBasket_WithValidId_Returns200AndBasketJson()
{
    // GET /api/v1/basket/basket-123 → 200 + { basketId, items[] }
}

[Fact]
public async Task GetBasket_WithInvalidId_Returns404()
{
    // GET /api/v1/basket/unknown-id → 404
}

[Fact]
public async Task AddItem_WithValidItem_Returns201AndUpdatedBasket()
{
    // POST /api/v1/basket/basket-123/items → 201 + { basketId, items[] }
}

[Fact]
public async Task AddItem_WithZeroQuantity_Returns400BadRequest()
{
    // POST /api/v1/basket/basket-123/items { quantity: 0 } → 400
}

[Fact]
public async Task CheckoutBasket_PublishesBasketCheckoutEvent()
{
    // POST /api/v1/basket/basket-123/checkout → 202 Accepted
    // Verify BasketCheckoutEvent published to message bus
}
```

---

### Ordering Service

| Endpoint | Method | Route | Handler | Success Scenario | Error Scenario |
|----------|--------|-------|---------|------------------|----------------|
| Get Order | GET | `/api/v1/orders/{id}` | `GetOrderById` | Return 200 + order JSON | 404 if not found |
| Create Order | POST | `/api/v1/orders` | `CreateOrder` | Return 201 + order JSON | 400 if items empty |
| Update Order | PUT | `/api/v1/orders/{id}` | `UpdateOrder` | Return 200 + updated order | 404 if not found |
| List Orders | GET | `/api/v1/orders?page=1&pageSize=10` | `ListOrders` | Return 200 + paginated orders | 400 if invalid page |
| Cancel Order | POST | `/api/v1/orders/{id}/cancel` | `CancelOrder` | Return 200 + cancelled order | 409 if already shipped |

#### Test Cases: Ordering Service Endpoints

```csharp
[Fact]
public async Task CreateOrder_WithValidItems_Returns201AndOrderId()
{
    // POST /api/v1/orders { customerId, items[] } → 201 + { orderId, ... }
}

[Fact]
public async Task CreateOrder_WithEmptyItems_Returns400BadRequest()
{
    // POST /api/v1/orders { customerId, items: [] } → 400
}

[Fact]
public async Task CancelOrder_AlreadyShipped_Returns409Conflict()
{
    // POST /api/v1/orders/order-123/cancel (status: shipped) → 409
}

[Fact]
public async Task ListOrders_ReturnsPagedResults()
{
    // GET /api/v1/orders?page=1&pageSize=10 → 200 + { items, total, pageNumber }
}
```

---

### Catalog Service

| Endpoint | Method | Route | Handler | Success Scenario | Error Scenario |
|----------|--------|-------|---------|------------------|----------------|
| Get Product | GET | `/api/v1/catalog/products/{id}` | `GetProductById` | Return 200 + product JSON | 404 if not found |
| List Products | GET | `/api/v1/catalog/products?pageNumber=1&pageSize=10` | `ListProducts` | Return 200 + paginated list | 400 if invalid page |
| Get Product by Name | GET | `/api/v1/catalog/products/search?name={name}` | `GetProductByName` | Return 200 + product JSON | 404 if not found |

#### Test Cases: Catalog Service Endpoints

```csharp
[Fact]
public async Task GetProduct_WithValidId_Returns200AndProductJson()
{
    // GET /api/v1/catalog/products/prod-123 → 200 + { id, name, price, ... }
}

[Fact]
public async Task ListProducts_WithValidPagination_ReturnsPagedResults()
{
    // GET /api/v1/catalog/products?pageNumber=1&pageSize=10 → 200 + { items, total }
}

[Fact]
public async Task GetProductByName_WithValidName_Returns200AndProduct()
{
    // GET /api/v1/catalog/products/search?name=Nike%20Shoes → 200 + { id, name, ... }
}
```

---

## Message Handler Routing

### BasketCheckoutEvent → BasketCheckoutEventHandler

| Message Type | Handler | Expected Action | Route Validation |
|--------------|---------|-----------------|------------------|
| `BasketCheckoutEvent` | `BasketCheckoutEventHandler` (Ordering service) | Create Order from basket items | Event published by Basket → consumed by Ordering |
| `OrderCreatedEvent` | `OrderCreatedEventHandler` | Update inventory, send notification | Event published by Ordering → consumed by Catalog, Notification service |
| `OrderShippedEvent` | `OrderShippedEventHandler` | Update basket status | Event published by Ordering → consumed by Basket |

#### Test Cases: Message Routing

```csharp
[Fact]
public async Task BasketCheckoutEvent_IsRoutedToOrderingService()
{
    // Publish BasketCheckoutEvent { BasketId, CustomerId, Items[] }
    // Verify BasketCheckoutEventHandler in Ordering service invokes
    // Verify Order created in database
    // Verify OrderCreatedEvent published
}

[Fact]
public async Task BasketCheckoutEvent_WithNullItems_PublishesOrderCreatedWithPlaceholders()
{
    // Known gap: BasketCheckoutEvent has no Items payload
    // Verify handler creates order with hardcoded placeholder OrderItemDto values
    // Confirm this is a limitation requiring spec fix (see consolidation-validation.md)
}

[Theory]
[InlineData(0)] // Empty basket
[InlineData(5)] // Multiple items
public async Task BasketCheckoutEvent_WithVariousItemCounts_RoutedCorrectly(int itemCount)
{
    // Test message routing with different basket sizes
}

[Fact]
public async Task OrderCreatedEvent_PublishedAfterOrderCreation()
{
    // Verify Ordering service publishes OrderCreatedEvent after creating Order
    // Verify event contains correct OrderId, CustomerId, TotalPrice
}
```

---

### Event Handler Dependency Updates (Post-Wolverine Modernization)

After removing MediatR from Ordering.Application:

| Handler | Dependency | Before (MediatR) | After (Wolverine) | Test Focus |
|---------|------------|------------------|-------------------|------------|
| `BasketCheckoutEventHandler` | Sender | `ISender` (MediatR) | `IMessageBus` | Verify `IMessageBus` invoked instead of `ISender` |

#### Test Case: ISender → IMessageBus Migration

```csharp
[Fact]
public async Task BasketCheckoutEventHandler_UsesIMessageBus_NotISender()
{
    // Arrange: Mock IMessageBus (not ISender)
    var mockMessageBus = new Mock<IMessageBus>();
    var handler = new BasketCheckoutEventHandler(mockMessageBus.Object, /* other deps */);
    var @event = new BasketCheckoutEvent { BasketId = "basket-123", ... };

    // Act
    await handler.Handle(@event, CancellationToken.None);

    // Assert: Verify IMessageBus.Publish() called (not ISender.Send())
    mockMessageBus.Verify(
        x => x.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<DeliveryOptions>()),
        Times.Once);
}
```

---

## YARP Gateway Routing

### Routing Configuration

| Upstream Route | Downstream Service | Backend Endpoint | Auth Required | Rate Limit |
|---|---|---|---|---|
| `/api/basket/**` | Basket Service | `http://basket-api:5001` | Yes | 100 req/min |
| `/api/orders/**` | Ordering Service | `http://ordering-api:5002` | Yes | 100 req/min |
| `/api/catalog/**` | Catalog Service | `http://catalog-api:5003` | No | 200 req/min |
| `/api/discount/**` | Discount Service | `http://discount-grpc:5004` | No | 500 req/min |

#### Test Cases: YARP Gateway Routing

```csharp
[Fact]
public async Task Gateway_RoutesBasketRequest_ToBasketService()
{
    // GET /api/basket/basket-123 via YARP → routes to Basket Service
    // Verify response from Basket Service returned to client
}

[Fact]
public async Task Gateway_EnforcesAuthentication_OnBasketRoute()
{
    // GET /api/basket/basket-123 without auth → 401 Unauthorized
    // Verify YARP auth policy enforced
}

[Fact]
public async Task Gateway_RoutesCatalogRequest_NoAuthRequired()
{
    // GET /api/catalog/products without auth → 200 OK
    // Verify Catalog route does not require authentication
}

[Fact]
public async Task Gateway_RoutesOrderingRequest_ToOrderingService()
{
    // POST /api/orders with auth → routes to Ordering Service
    // Verify Order created response
}

[Theory]
[InlineData("http://basket-api:5001")]
[InlineData("http://ordering-api:5002")]
[InlineData("http://catalog-api:5003")]
public async Task Gateway_RoutesSuccessfully_ToBackend(string expectedBackend)
{
    // Test upstream → downstream routing for each service
}
```

---

### Authentication & Authorization Enforcement

| Route | Auth Policy | Expected Behavior | Test Scenario |
|-------|-------------|-------------------|---------------|
| `/api/basket/**` | Bearer JWT | Only authenticated users | Test with/without valid token |
| `/api/orders/**` | Bearer JWT + Customer role | Only customers | Test with customer/admin token |
| `/api/catalog/**` | Public | Anonymous access | No auth required |

#### Test Cases: Auth Enforcement

```csharp
[Fact]
public async Task Gateway_WithValidJwt_AllowsBasketAccess()
{
    // GET /api/basket/basket-123 + valid JWT → 200 OK
}

[Fact]
public async Task Gateway_WithExpiredJwt_Returns401Unauthorized()
{
    // GET /api/basket/basket-123 + expired JWT → 401 Unauthorized
}

[Fact]
public async Task Gateway_WithoutJwt_Returns401Unauthorized()
{
    // GET /api/basket/basket-123 (no auth header) → 401 Unauthorized
}

[Fact]
public async Task Gateway_AllowsAnonymousAccessToCatalog()
{
    // GET /api/catalog/products (no auth) → 200 OK
}
```

---

## gRPC Service Routing

### Discount Service (gRPC)

| Service Method | Request | Response | Test Scenario |
|---|---|---|---|
| `GetDiscount` | `GetDiscountRequest { productName: string }` | `GetDiscountResponse { discount: int }` | Look up discount by product name |

#### Test Cases: gRPC Routing

```csharp
[Fact]
public async Task DiscountService_GetDiscount_WithValidProductName_ReturnsDiscount()
{
    // Call gRPC GetDiscount(productName: "Nike Shoes") → returns discount: 20
}

[Fact]
public async Task DiscountService_GetDiscount_WithUnknownProductName_ReturnsZeroDiscount()
{
    // Call gRPC GetDiscount(productName: "UnknownProduct") → returns discount: 0
}

[Fact]
public async Task BasketService_CallsDiscountService_ByProductName()
{
    // Verify Basket service gRPC calls Discount service by ProductName (not ProductId)
    // See consolidation-validation.md: current limitation is ProductName lookup
}
```

---

## Routing Test Implementation Checklist

- [ ] All API endpoints tested (success + error paths)
- [ ] Message handlers verified to receive correct event types
- [ ] Event routing tested end-to-end (publish → handler invocation)
- [ ] YARP routes map upstream paths to downstream services correctly
- [ ] Authentication enforced on protected routes
- [ ] gRPC service discovery and routing verified
- [ ] Fallback/retry logic tested for failed backend calls
- [ ] Rate limiting tested (if configured)
- [ ] Request headers propagated correctly through gateway
- [ ] Cross-service event ordering tested (if applicable)
