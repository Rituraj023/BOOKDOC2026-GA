# 39 - Admin Security Foundation Reference Slice

Owner: Admin + Identity + security + operations  
Status: Verified reference slice  
Reviewed: 2026-08-18

## Outcome

BOOKDOC2026 Admin now has a working security foundation around the existing API-owned Identity and JWT flows:

```text
approved client network
  -> fail-closed Admin network middleware
  -> interactive server-side Blazor circuit
  -> API login / refresh rotation / revoke
  -> permission-filtered navigation and workspace sections
  -> API remains the final authorization authority
```

This slice changes the Admin host and shared typed Auth Client only. It does not create another identity system, copy legacy authentication, add a browser token store, or weaken API permissions.

## Server-side session behavior

`AdminSessionState` is scoped to the interactive server circuit and implements the existing `IBookDocTokenStore` contract. Access and refresh tokens remain in server process memory; Admin does not write them to browser local storage, session storage, cookies or JavaScript.

The implemented lifecycle is:

1. the login page submits credentials and an optional durable clinic scope to the API;
2. the API returns the authoritative display identity, scope, roles, permissions and rotating tokens;
3. the Admin circuit retains that session metadata and token pair in memory;
4. an operation refreshes when the access token has one minute or less remaining;
5. a successful refresh atomically replaces token and permission state;
6. an authentication/authorization rejection during refresh clears both tokens and presents an expired-session route;
7. sign-out calls the API revoke route with the current bearer token and clears local state even if the remote revoke fails.

Browser reload currently creates a new circuit and therefore requires sign-in again. This conservative behavior avoids introducing a durable browser credential before the production session-persistence, Data Protection and privileged-session policy is approved. It is an explicit remaining UX decision, not silent token loss.

## Authorization-aware Admin UI

The Admin layout renders protected navigation only after authentication. The Communications workspace:

- denies direct page use when no communication permission is present;
- loads only the API collections permitted to the signed-in account;
- separates template view, manage and publish actions;
- separates preference view and manage behavior;
- renders callback and delivery monitoring only with their view permissions;
- distinguishes an expired/invalid session from a forbidden operation.

This filtering improves usability and reduces accidental forbidden requests. It is not a security boundary by itself: every API operation continues to enforce its named permission and durable tenant/organization/branch scope.

## Approved-network policy

`AdminNetworkRestrictionMiddleware` runs before the Admin UI pipeline. Production defaults to enforcement with an empty allow-list, which intentionally denies access until deployment supplies approved networks.

Configuration shape:

```json
{
  "AdminNetwork": {
    "Enforce": true,
    "AllowedNetworks": [ "203.0.113.0/24", "2001:db8:1234::/48" ],
    "TrustedProxies": [ "10.0.0.10/32" ]
  }
}
```

The addresses above are documentation-only reserved examples. No deployment endpoint or credential is committed.

Policy rules:

- IPv4 and IPv6 CIDR ranges are supported;
- missing remote address or an empty allow-list fails closed;
- `X-Forwarded-For` is ignored unless the immediate peer belongs to `TrustedProxies`;
- trusted chains are resolved from the nearest hop toward the original client, with a maximum of ten entries;
- malformed chains and malformed configured CIDRs fail closed;
- enforcement can be disabled only in Development or Testing;
- denial logging records a reason code, not the client address or forwarded chain;
- `/health` and `/alive` remain data-free orchestration endpoints and bypass this UI access policy.

Production operations must supply the real allow-list and proxy topology through environment/deployment configuration and test from an authorized and unauthorized network before release.

## Changed implementation surfaces

- `BookDoc2026.Client/AuthApiClient.cs` now supports authorized session revocation.
- `BookDoc2026.Admin/Security/AdminSessionState.cs` owns circuit-scoped sign-in, refresh, expiry and sign-out state.
- `BookDoc2026.Admin/Security/AdminNetworkPolicy.cs` supplies testable CIDR and trusted-proxy evaluation.
- `BookDoc2026.Admin/Security/AdminNetworkRestrictionMiddleware.cs` enforces the host boundary.
- `BookDoc2026.Admin/Security/AdminPermissionPolicy.cs` centralizes communication-workspace navigation eligibility.
- Admin routing, layout, login, home and communication screens now use interactive server session state and permission-aware rendering.
- Admin configuration defaults to fail closed outside development.

No database table, EF migration, public API route, package or production secret was added.

## Verification evidence

Verified on 2026-08-18:

```text
Unit tests:                         42 passed
Architecture tests:                  9 passed
Integration tests:                  30 passed
Total automated tests:              81 passed
Full solution build:                passed, 0 warnings / 0 errors
Admin build:                        passed, 0 warnings / 0 errors
Development home/login smoke:       HTTP 200
Production empty-list home smoke:   HTTP 403
Production health smoke:            HTTP 200
```

The nine new unit tests prove:

- empty allow-list denial;
- direct IPv4 CIDR acceptance;
- forwarded-header spoofing is ignored from an untrusted peer;
- a trusted multi-proxy chain resolves the original approved client;
- invalid CIDR configuration is rejected without echoing its value;
- navigation requires at least one relevant communication permission;
- login plus near-expiry token rotation updates tokens and permissions;
- rejected refresh clears credentials and marks the session expired;
- sign-out sends the bearer token to revoke and clears the local session.

The existing API integration tests continue to prove permission and durable-scope enforcement. Browser automation, a deployed reverse-proxy test and an external network test remain required before production.

## Achievement impact

This slice advances the evidence-based DOC-033 rows as follows:

- row 6, Admin, from 40% to 55% because sign-in/session UX, permission-aware navigation and a fail-closed network boundary now work;
- row 10, shared Client/Contracts, from 70% to 75% because Admin now exercises login, refresh rotation and revoke through the shared Auth Client;
- row 11, Identity, from 80% to 85% because the Admin host now consumes API-owned session rotation, expiry and revocation correctly.

At this slice's checkpoint, the exact twenty-row simple average was **62%**. [DOC-040](40-generalized-booking-and-preference-notification-reference-slice.md) subsequently advances the current score to 63%. No score from this Admin slice is awarded for MFA, durable privileged browser sessions, production proxy deployment, report/control-plane screens, accessibility/browser automation, provider operations or user acceptance.

## Remaining gates

Before production use:

1. approve the Admin durable-session versus re-authentication policy, including idle/absolute timeout and privileged re-authentication;
2. approve MFA and privileged-access requirements;
3. configure production signing keys and shared Data Protection operations;
4. supply and independently verify real allowed networks and trusted proxies;
5. add browser accessibility and expired-session interaction tests;
6. add deployed reverse-proxy tests proving scheme forwarding and header-spoof resistance;
7. implement session/device administration and access-review operations.

## Next recommended slice

[DOC-040](40-generalized-booking-and-preference-notification-reference-slice.md) completes this recommended backend slice with atomic generalized Booking and preference-controlled durable patient notification. The next recommendation is Booking cancellation, rescheduling and waitlist promotion; production providers remain gated by DOC-017 and DOC-020.
