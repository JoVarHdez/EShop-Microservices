# Pending Features (Prioritized by Importance and Impact)

## Priority 0 (Must Do First): Missing IAM Capability

### Why First
- This is a hard dependency for secure APIs, identity propagation, and any client-facing AI feature.
- Without this, service-to-service trust remains weak and authorization is inconsistent.

### Current Gap
- No centralized identity and access management capability is integrated.
- APIs and web app do not consistently enforce authenticated identity and authorization policies.
- Service interactions rely on trust-by-input patterns in several places.

### Required IAM Features
- Central identity provider integration (for example Entra ID, Auth0, Keycloak, or equivalent).
- Authentication for web app and APIs.
- Authorization policies per service and endpoint.
- Claims-based user identity propagation from gateway to services.
- Service-to-service authentication for internal calls/events where applicable.
- Standardized token validation, issuer and audience checks, and role and scope enforcement.

## Priority 1 (Do Immediately After IAM): Cross-Service Connection Gaps to Close

### Why Second
- These are correctness and integration blockers for checkout and downstream workflow quality.

- Catalog to Discount: no strong product identity contract yet.
- Basket to Ordering: checkout mapping still includes hardcoded order item placeholders.
- Shopping to Basket and Ordering: identity is not sourced from IAM and is still partly hardcoded.
- Gateway to downstream services: no centralized auth enforcement and identity-forwarding policy baseline.

## Priority 2: Inventory Service

### Why Third
- High business impact: prevents overselling and stabilizes checkout behavior.

- Purpose: track stock per product and prevent overselling during checkout.
- Key capabilities:
	- Reserve stock when checkout starts.
	- Confirm reservation when payment succeeds.
	- Release reservation when payment or order fails.
	- Support manual stock adjustments and low-stock thresholds.
- Suggested events:
	- InventoryReserved
	- InventoryReservationFailed
	- InventoryReleased
	- StockAdjusted

## Priority 3: Payment Service

### Why Fourth
- Revenue-critical and tightly coupled to order state transitions.

- Purpose: manage authorization, capture, and refund lifecycle for orders.
- Key capabilities:
	- Authorize payment on checkout.
	- Capture payment when order transitions to confirmed.
	- Trigger refunds for canceled or failed fulfillment flows.
	- Add retries and dead-letter handling for provider failures.
- Suggested events:
	- PaymentAuthorized
	- PaymentFailed
	- PaymentCaptured
	- PaymentRefunded

## Priority 4: Audit and Compliance Service

### Why Fifth
- Critical for traceability, IAM hardening, and incident/compliance readiness.

- Purpose: maintain immutable audit records for security and business actions.
- Key capabilities:
	- Append-only audit event store.
	- Correlation by user, service, action, and trace id.
	- Retention and export policies for compliance scenarios.
	- Query APIs for audit review and incident investigations.
- Suggested events:
	- AuditEntryRecorded
	- AuditExportRequested

## Priority 5: Notification Service

### Why Sixth
- Important for customer experience, but not a core transaction blocker.

- Purpose: centralize outbound customer notifications across channels.
- Key capabilities:
	- Email, SMS, and push notification dispatch.
	- Template-based notifications per event type and locale.
	- Delivery retry and deduplication strategy.
	- User notification preferences and opt-out support.
- Suggested events:
	- NotificationSent
	- NotificationFailed

## Priority 6: Review and Rating Service

### Why Seventh
- Product-growth feature with moderate impact compared to checkout-critical services.

- Purpose: capture product reviews and ratings with moderation support.
- Key capabilities:
	- Create, update, and delete customer reviews.
	- Product rating aggregation and review summaries.
	- Moderation workflow for flagged content.
	- Verified purchase checks through Ordering integration.
- Suggested events:
	- ReviewSubmitted
	- ReviewModerated
	- ProductRatingUpdated

## Priority 7: AI Assistance Service for Clients

### Why Last
- High potential impact, but depends on IAM maturity, stable service contracts, and strong guardrails.

- Purpose: provide an authenticated assistant for customer support and shopping help.
- Key capabilities:
	- Answer policy, catalog, and order-status questions.
	- Context-aware retrieval over catalog and help documentation.
	- Personalized recommendations using user activity and purchase history.
	- Human handoff path for unresolved or sensitive requests.
- Guardrails and security baseline:
	- Enforce IAM claims for user-scoped data access.
	- Add prompt safety and content filtering.
	- Redact sensitive data in logs and telemetry.
	- Restrict tool access and rate-limit assistant endpoints.

## Recommended Execution Sequence

1. Priority 0: Missing IAM Capability
2. Priority 1: Cross-Service Connection Gaps
3. Priority 2: Inventory Service
4. Priority 3: Payment Service
5. Priority 4: Audit and Compliance Service
6. Priority 5: Notification Service
7. Priority 6: Review and Rating Service
8. Priority 7: AI Assistance Service for Clients
