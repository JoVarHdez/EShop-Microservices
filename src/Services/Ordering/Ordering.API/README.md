# Ordering.API

HTTP entry point for the Ordering microservice. This project now uses native Minimal API route groups and Wolverine message dispatch.

## Target Framework & Dependencies

- .NET 10.0
- WolverineFx.FluentValidation 5.39.1
- Mapster
- AspNetCore.HealthChecks.SqlServer 9.0.0
- AspNetCore.HealthChecks.UI.Client 9.0.0
- Microsoft.EntityFrameworkCore.Design 10.0.8 (build-time only)
- Project references: Ordering.Application, Ordering.Infrastructure

## Project Structure

```
Ordering.API/
├── Program.cs
├── DepedencyInjection.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── Properties/
│   └── launchSettings.json
└── Endpoints/
    ├── OrdersEndpoints.cs
    ├── CreateOrder.cs
    ├── UpdateOrder.cs
    ├── DeleteOrder.cs
    ├── GetOrders.cs
    ├── GetOrdersByCustomer.cs
    └── GetOrdersByName.cs
```

## Bootstrap

Program wiring is centered around three service-layer registrations and Wolverine host integration:

```csharp
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);
});
```

On startup, API middleware maps endpoints and enables global exception + health checks through UseApiServices.

## API Registration

### AddApiServices

- Registers CustomExceptionHandler.
- Registers SQL Server health check using ConnectionStrings:Database.

### UseApiServices

- Maps all Ordering routes through MapOrdersEndpoints.
- Enables UseExceptionHandler.
- Exposes GET /health with HealthChecks.UI.Client response writer.

## Routing Model

- OrdersEndpoints.cs defines app.MapGroup("/orders").
- Each endpoint file contributes one extension method on RouteGroupBuilder.
- Route fragments are composed under the /orders group.

## Endpoints

### POST /orders

- Request: CreateOrderRequest containing OrderDto.
- Handler dispatch: IMessageBus.InvokeAsync<CreateOrderResult>.
- Responses: 201 Created, 400 Bad Request.

### PUT /orders

- Request: UpdateOrderRequest containing OrderDto.
- Handler dispatch: IMessageBus.InvokeAsync<UpdateOrderCommandResult>.
- Result mapping:
  - UpdateOrderResult -> 200 OK
  - UpdateOrderNotFound -> 404 Not Found
- Responses: 200 OK, 404 Not Found, 400 Bad Request.

### DELETE /orders/{id}

- Handler dispatch: IMessageBus.InvokeAsync<DeleteOrderCommandResult>.
- Result mapping:
  - DeleteOrderResult -> 200 OK
  - DeleteOrderNotFound -> 404 Not Found
- Responses: 200 OK, 404 Not Found, 400 Bad Request.

### GET /orders

- Query params: pageIndex, pageSize.
- Dispatch model: direct DI into GetOrdersHandler (no message-carrier query type).
- Response: 200 OK with paginated orders.

### GET /orders/customer/{customerId}

- Dispatch model: direct DI into GetOrderByCustomerHandler.
- Response: 200 OK with customer orders.

### GET /orders/{orderName}

- Dispatch model: direct DI into GetOrdersByNameHandler.
- Response: 200 OK with name-filtered orders.

## Run Locally

```bash
dotnet run --project src/Services/Ordering/Ordering.API
```

Development environment still initializes the Ordering database automatically.

## Related Implementation Files

- [Program.cs](Program.cs)
- [DepedencyInjection.cs](DepedencyInjection.cs)
- [Endpoints/OrdersEndpoints.cs](Endpoints/OrdersEndpoints.cs)
- [Endpoints/CreateOrder.cs](Endpoints/CreateOrder.cs)
- [Endpoints/UpdateOrder.cs](Endpoints/UpdateOrder.cs)
- [Endpoints/DeleteOrder.cs](Endpoints/DeleteOrder.cs)
- [Endpoints/GetOrders.cs](Endpoints/GetOrders.cs)
- [Endpoints/GetOrdersByCustomer.cs](Endpoints/GetOrdersByCustomer.cs)
- [Endpoints/GetOrdersByName.cs](Endpoints/GetOrdersByName.cs)
