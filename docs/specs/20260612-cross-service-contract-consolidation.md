# Cross-Service Contract Consolidation (Microservices Preserved)

## 1. Feature Summary

This feature consolidates the cross-service contracts between Catalog, Discount, Basket, Ordering, Shopping.Web.Razor, and Gateway so the existing microservices architecture remains intact but key integrations stop relying on placeholders, weak identity keys, and inconsistent user context assumptions. The goal is to make checkout and downstream workflows correct and deterministic without reworking the system into a monolith and without introducing IAM in this phase.

## 2. Data Model / Entities

### ProductIdentityContract
- ProductId: Guid - canonical product identity shared across Catalog, Discount, Basket, and Ordering.
- ProductName: string - human-readable label; not the canonical cross-service key.
- SourceService: string - owning service for the product record.

### DiscountLookupContract
- ProductId: Guid - required lookup key for discount resolution.
- ProductName: string? - optional compatibility field for migration visibility.
- DiscountAmount: decimal - resolved discount value.
- Found: bool - whether a discount rule exists for the requested product.

### BasketCheckoutLineItem
- ProductId: Guid - item identity from basket/catalog.
- ProductName: string - product display value at checkout time.
- Quantity: int - requested quantity.
- UnitPrice: decimal - per-item price snapshot at checkout.

### BasketCheckoutContract
- UserName: string - user identifier used by checkout and order creation for this phase.
- CustomerId: Guid - customer identifier used by ordering queries and command handling.
- TotalPrice: decimal - full checkout amount.
- Items: List<BasketCheckoutLineItem> - required item payload for downstream order creation.
- AddressAndPaymentData: object - existing checkout address/payment payload preserved in this phase.

### OrderCreationContract
- OrderId: Guid - created order identifier.
- CustomerId: Guid - copied from checkout contract.
- OrderName: string - user-scoped order name.
- OrderItems: list - line items mapped from checkout contract items (no placeholders).

### DevelopmentUserContextContract
- UserName: string - consistent user name source used across shopping, basket, and ordering flows.
- CustomerId: Guid - consistent customer ID source used across shopping, basket, and ordering flows.
- SourceType: string - where this value comes from in this phase (configuration/stub/service).

## 3. Business Rules & Constraints

The system MUST enforce the following non-negotiable rules:

1. The solution MUST remain microservices-based; this feature is contract consolidation, not service consolidation into one host.
2. Cross-service product identity MUST be based on ProductId and MUST NOT depend on ProductName as the authoritative integration key.
3. Basket checkout payloads MUST include real line items so Ordering can create orders from actual basket contents.
4. Ordering MUST NOT use hardcoded placeholder order items during checkout-based order creation.
5. Discount resolution contract with Basket MUST align to ProductId-based lookup semantics.
6. Shopping.Web.Razor, Basket, and Ordering MUST use one consistent development user context source for this phase.
7. Gateway authentication and identity-forwarding policy baseline are explicitly deferred in this phase and MUST NOT block contract consolidation.
8. Existing route topology and service boundaries MAY remain unchanged as long as the contract behavior is corrected.
9. Contract changes MUST preserve backward compatibility strategy where needed during transition (versioning or temporary compatibility fields).
10. Contract consolidation in this phase MUST not include implementation of Inventory, Payment, Notification, Audit/Compliance, Review/Rating, or AI Assistant services.

## 4. Acceptance Criteria

The feature is complete when:

- [ ] A formal ProductId-based contract exists between Catalog and Discount integration points and is approved.
- [ ] Basket-to-Ordering checkout contract includes a required items payload containing ProductId, ProductName, Quantity, and UnitPrice.
- [ ] The order creation flow definition no longer permits hardcoded placeholder order item values.
- [ ] Shopping.Web.Razor to Basket and Ordering flow uses one defined development user context contract instead of scattered hardcoded values.
- [ ] Priority 1 contract gaps from `docs/PendingFeatures.md` are represented in this spec, with IAM/gateway auth enforcement explicitly deferred.
- [ ] The additional missing structural gap is included: checkout contract must carry line items for downstream correctness.
- [ ] A backward-compatibility decision is captured for migrating from ProductName-based discount lookups to ProductId-based lookups.
- [ ] The resulting contract set is sufficient to support downstream Priority 2 (Inventory Service) without introducing new placeholder data paths.

## 5. Out of Scope

The following are explicitly NOT part of this feature:

- Central identity provider integration and full IAM rollout.
- Gateway authentication enforcement, token validation, and claims-forwarding policy implementation.
- Service-to-service auth hardening and trust boundary redesign.
- Runtime migration from microservices to monolith/modular monolith.
- Infrastructure replatforming, deployment topology redesign, or service host merger.
- Implementation planning and coding steps.

## 6. Open Questions

1. **Should ProductName remain in DiscountLookupContract during the transition period?**
   - Context: ProductName is currently used in live code paths, while ProductId is the target authoritative key.
   - Options:
     - A) Keep ProductName as optional compatibility field for one release window
     - B) Remove ProductName immediately and require ProductId-only lookup
   - Decision: A) Keep ProductName as optional compatibility field for one release window.
   - Implication: lowers migration risk while establishing ProductId as the authority.

2. **What is the temporary source of DevelopmentUserContextContract before IAM is introduced?**
   - Context: Current code uses hardcoded values in multiple places, causing inconsistency.
   - Options:
     - A) Centralized app configuration values per environment
     - B) One shared in-process user-context provider with fixed dev defaults
     - C) Keep scattered hardcoded values until IAM arrives
   - Decision: C) Keep hardcoded values until IAM arrives, but maintain consistency through shared hardcoded defaults where needed.
   - Implication: avoids introducing new identity infrastructure now while still preventing drift between Basket and Ordering values.

3. **How should checkout contract changes be versioned across Basket and Ordering?**
   - Context: Adding required line items may impact current consumers or tests.
   - Options:
     - A) In-place contract update with coordinated deploy
     - B) Versioned contract (`v2`) with temporary dual-read support
   - Decision: A) In-place contract update with coordinated deploy.
   - Implication: fastest approach for a local learning project with no tests yet, with lower migration overhead.

4. **Should gateway auth-related gaps remain listed under Priority 1 if auth is deferred for this learning phase?**
   - Context: The team wants to ignore auth for now but still track cross-service correctness.
   - Options:
     - A) Keep gateway auth gap in Priority 1 with explicit deferred status
     - B) Move gateway auth gap back under Priority 0 only
  - Decision: A) Keep it in Priority 1 with explicit deferred status.
   - Implication: preserves visibility without blocking contract consolidation delivery.