# Test Plan Template

Use this structure when generating test plans from specifications.

## Header

```markdown
# Test Plan: [Feature Name]

**Spec**: [Link to spec file](../../specs/[YYYYMMDD]-[feature-name].md)
**Service**: [Service name]
**Framework**: xUnit + Moq
**Coverage Target**: 90% line coverage, 70%+ branch coverage on critical paths
**Last Updated**: [YYYY-MM-DD]
```

## Overview

Brief summary of:
- Feature/service being tested
- Key responsibilities (business logic, integrations, routing)
- Scope and constraints
- External dependencies (gRPC, MassTransit, database, etc.)

## Unit Tests

### Category: [Business Logic / Handler / Validator]

#### Test Case: [Scenario]
- **Class**: `[HandlerName]Tests` or `[EntityName]Tests`
- **Method**: `[MethodName]_[Condition]_[ExpectedResult]`
- **Arrangement**: Setup objects, mocks, test data
- **Action**: Call the method/handler
- **Assertion**: Verify output, side effects, calls to mocks
- **Dependencies to Mock**: [List external dependencies]

---

## Integration Tests

### Message Handler Routing

#### Test Case: [Event Type] → [Handler]
- **Message Type**: `[EventName]`
- **Handler**: `[EventHandlerName]`
- **Setup**: Create event with sample data
- **Action**: Dispatch via `IMessageBus` (Wolverine) or `IPublishEndpoint` (MassTransit)
- **Assertion**: Handler invoked, repository updated, downstream event published
- **External Calls**: List any gRPC/HTTP calls mocked or stubbed

#### Dependencies to Mock
- Database (`DbContext`)
- Message bus (Wolverine `IMessageBus`)
- External services (gRPC clients)

---

### API Endpoint Integration

#### Test Case: [HTTP Method] [Route] → [Handler] → [Response]
- **Endpoint**: `[Method] /api/[resource]/[action]`
- **Request Contract**: Input DTO fields
- **Handler**: `[ControllerName].[ActionName]` or minimal API delegate
- **Response Contract**: Output DTO, status code
- **Setup**: Create test data, mock external dependencies
- **Action**: Call endpoint with test request
- **Assertion**: Response status, body contract, side effects
- **Authorization**: Identity/claims requirements (if YARP enforced)

---

## Routing Tests

### YARP Gateway Routing

#### Test Case: [Route] → [Backend Service]
- **Gateway Route**: Route rule in YARP config
- **Upstream**: Request path/method at gateway
- **Downstream**: Backend service endpoint
- **Authentication**: Claims/roles enforced
- **Setup**: Gateway running, backend service mock
- **Action**: Send request to gateway
- **Assertion**: Request routed to correct backend, auth enforced
- **Coverage**: Happy path, auth failures, 404s

#### Dependencies
- YARP configuration (`appsettings.json`)
- Auth policy (if any)
- Backend service stubs

### Message Handler Routing

#### Test Case: [Event Type] dispatched → [Handler invoked]
- **Message Type**: `[EventName]`
- **Handler Target**: `[EventHandlerClass]`
- **Setup**: Configure message routing (Wolverine/MassTransit)
- **Action**: Publish event with test data
- **Assertion**: Correct handler invoked, message not lost
- **Coverage**: Handler selection, ordering, retry logic

### gRPC Service Routing

#### Test Case: [Service Method] → [Backend gRPC endpoint]
- **Service**: `[ServiceName].Grpc`
- **Method**: `[MethodName]`
- **Request**: Protobuf message
- **Response**: Protobuf message or error
- **Setup**: gRPC server mock or TestServer
- **Action**: Call gRPC method
- **Assertion**: Response contract, error handling
- **Coverage**: Success, validation errors, service unavailable

---

## Code Coverage Targets

- **Line Coverage**: 90% minimum per service
- **Branch Coverage**: 70%+ on critical paths (event handlers, aggregates, validators)
- **Excluded**: Auto-generated code (gRPC stubs), EF Core migrations

### Coverage Validation

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Verify coverage exceeds targets
coverlet . --target "dotnet" --targetargs "test" --format opencover --output coverage.xml
```

---

## Dependencies to Mock

List all external dependencies and mocking strategy:

| Dependency | Type | Mock Strategy | Example |
|------------|------|---|---------|
| DbContext | Database | In-memory or fake implementation | `var mockDb = new Mock<OrderingContext>()` |
| gRPC Client | External Service | Moq + callback, or gRPC test fixture | `mockDiscountClient.Setup(x => x.GetDiscount(...))` |
| IMessageBus | Wolverine | Mock or TestServer | `new Mock<IMessageBus>()` |
| IPublishEndpoint | MassTransit | Mock or LocalTransport | `new Mock<IPublishEndpoint>()` |
| ILogger | Logging | Moq (NSubstitute alternative) | `new Mock<ILogger<MyHandler>>()` |

---

## Test Implementation Checklist

- [ ] Unit tests written for all business logic
- [ ] Handler tests verify correct behavior with mocked dependencies
- [ ] Integration tests validate message routing end-to-end
- [ ] API endpoint tests verify contract and authorization
- [ ] YARP gateway routing verified with upstream/downstream assertions
- [ ] Code coverage ≥ 90% (use Coverlet to measure)
- [ ] Branch coverage ≥ 70% on critical paths
- [ ] All external service calls mocked (no real gRPC/database calls)
- [ ] Test project references SUT (system under test) correctly
- [ ] Test names follow convention: `[Method]_[Condition]_[ExpectedResult]`
- [ ] Arrange-Act-Assert structure clear in each test
- [ ] Async tests use `async Task` and `await`
- [ ] Disposable mocks cleaned up (or use fixture)
