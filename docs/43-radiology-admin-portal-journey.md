# 43 — Radiology Admin and Portal Journey

Owner: Admin + Portal + shared UI + Queue + security  
Status: Verified web reference journey  
Reviewed: 2026-08-23

## Outcome

This slice turns the X-ray/CT Queue backend into the first role-facing web journey:

- approved-network Admin users with both `Queues.ServicePoints.Manage` and `Queues.View` can create and inspect branch imaging service points;
- authenticated Portal users with `Queues.View` can select a service point and reconcile its technician worklist;
- action buttons are independently composed from `Queues.Call`, `Queues.Progress` and `Queues.Cancel`;
- `Queues.Display.View` reveals only the safe token/status/call-count display projection;
- shared `BookDoc2026.Blazor.UI` components render the worklist, display and status vocabulary;
- SignalR is a branch-authorized invalidation channel and the API remains authoritative.

This is not a clinical radiology information system. It does not store orders, protocols, radiation/safety checks, DICOM/PACS data, findings, signed reports or billing.

## Admin boundary

`/radiology/service-points` runs inside the existing Interactive Server Admin host. Credentials and tokens remain in the server circuit, Admin network restriction executes before pages, and navigation is hidden unless the two required permissions are present. The page defaults to the signed-in branch scope but permits an authorized protected branch ID for multi-branch administrators.

The new catalog read endpoint is protected by `Queues.View`; service-point creation remains protected by `Queues.ServicePoints.Manage`. Admin deliberately requires both so a manager never receives a create form whose resulting catalog cannot be read.

## Portal boundary

Portal now has a real in-memory authenticated session with login, near-expiry refresh, revoke/sign-out and permission state. Access and refresh tokens are not persisted in local storage, session storage or cookies; a browser refresh intentionally requires sign-in again. This is a safe reference policy, not the final user-convenience/session-device policy.

The Portal worklist:

1. uses the protected branch scope issued by authentication;
2. loads active service points through the typed Client;
3. loads full technician and safe-display projections only when separately permitted;
4. sends versioned transition commands through the API;
5. refetches after successful mutations, conflicts and reconnect;
6. retains a manual Refresh path when live connectivity is unavailable.

The current session is in-memory only. MFA, durable device/session administration, multi-tab coordination and final browser threat-model acceptance remain open.

## Shared UI and realtime rule

`RadiologyWorklist`, `QueueDisplayBoard` and `QueueStatusBadge` contain presentation and permission-conditioned affordances, not database or business logic. `RadiologyQueueProjection` replaces the worklist and safe display from one API reconciliation pass.

Protected public IDs are encrypted strings whose encryption is probabilistic. Two strings can validly represent the same numeric identity while being unequal ciphertext. Therefore the browser never compares service-point or ticket ciphertext from a SignalR event with ciphertext from an earlier API response. Any well-formed event delivered through the server-derived authorized branch group invalidates the selected projection, which is then safely filtered again by the API.

The API accepts `access_token` from the query string only for `/hubs/queue`, as required by browser WebSocket transport. No other API path gains query-token authentication. Cross-origin Portal calls use a fail-closed configured production origin allow-list; development additionally permits loopback origins so AppHost's dynamic localhost ports work without opening arbitrary production origins.

## Verification

Verified on 2026-08-23:

```text
Full solution build:                      passed, 0 warnings / 0 errors
Unit tests:                               54 passed
Architecture tests:                        9 passed
Integration tests:                        32 passed
Total automated tests:                    95 passed
EF pending-model-change check:             passed (none pending)
```

New evidence covers shared projection replacement, ciphertext-safe SignalR invalidation, independent Admin navigation permission composition, service-point catalog authorization and typed catalog response. Existing Queue integration evidence continues to cover mutations, terminal states, privacy projection, tenancy and hub negotiation.

No deployed browser/device acceptance is claimed. At this checkpoint the Queue migration remained generated but unapplied; [DOC-044](44-permission-dashboards-and-radiology-reception.md) subsequently records its corrected local-development application. No clinic credentials, migrated clinic data, production SQL deployment, proxy deployment, two-browser concurrency, reconnect fault injection, accessibility audit or user acceptance was available in this session.

## Completion percentage

The following DOC-033 rows advance:

- row 6, Admin: **55% → 60%**;
- row 7, Portal: **40% → 50%**;
- row 9, shared UI: **40% → 50%**;
- row 16, dashboards/Queue/realtime: **50% → 65%**.

The twenty-row score gains 40 points: `1335 / 20 = 66.75%`.

- exact architecture-achievement completion: **66.75%**;
- rounded headline completion: **67%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

Row 20 remains 70% despite three additional unit tests and stronger integration coverage because browser, deployed SQL, security, load, recovery and UAT evidence are still absent.

## Recommended next goal

Status on 2026-08-23: dashboard composition, reception check-in and the local migration rehearsal are implemented in [DOC-044](44-permission-dashboards-and-radiology-reception.md). Browser automation remains open because no controlled role fixtures were provisioned.

Complete the missing front of this operational journey:

1. add shared permission-composed dashboard cards for Admin and Portal roles;
2. add a Portal reception imaging check-in flow using authorized patient search/selection, optional Booking context and idempotent request IDs;
3. show the newly checked-in safe token without leaking patient identity to the display;
4. add loading, duplicate-submit, permission, conflict and retry states;
5. run the journey against an applied development SQL migration with real browser automation and two-session SignalR reconnect tests.

This closes reception → technician → display behavior before introducing clinical imaging order/report data whose terminology and safety rules require named clinical approval.
