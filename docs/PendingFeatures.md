## Missing IAM Capability

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

### Suggested Rollout Phases
1. Foundation:
	- Choose IAM provider, define token model, configure gateway token validation.
2. Service enforcement:
	- Add authentication and authorization middleware and policies in Catalog, Basket, Ordering, and Discount.
3. Identity propagation:
	- Replace userName and customerId caller-trust patterns with claims-based identity resolution.
4. Hardening:
	- Add policy tests, audit logs, and least-privilege route protections.
5. Cleanup:
	- Remove demo identity assumptions, hardcoded users, and non-production fallback behavior.

## Cross-Service Connection Gaps to Close

- Catalog to Discount: no strong product identity contract yet.
- Basket to Ordering: checkout mapping still includes hardcoded order item placeholders.
- Shopping to Basket and Ordering: identity is not sourced from IAM and is still partly hardcoded.
- Gateway to downstream services: no centralized auth enforcement and identity-forwarding policy baseline.
