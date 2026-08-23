# 42 — Imaging Queue and Realtime Reference Slice

Owner: Queue + Radiology + API + Portal foundation + security
Status: Verified backend/realtime reference slice
Reviewed: 2026-08-23

## Outcome

This slice implements the first reusable departmental Queue for X-ray and CT. It introduces imaging service points, idempotent patient check-in, priority control, versioned workflow transitions, append-only queue events, privacy-safe display projections, authorized reconciliation APIs and transient SignalR invalidation after a database commit.

Queue is a separate module. It does not mutate Booking status, store diagnostic images, execute clinical reporting or use SignalR as a command or audit channel.

## First workflow

```text
Waiting -> Called -> Preparation -> InService -> Completed
    |         |            |             |
    +---------+------------+-------------+-> Cancelled
```

A called ticket may be recalled without changing its status. Recall increments the ticket version and call count and creates another append-only event. Invalid stage skips, terminal changes and stale versions fail closed.

The current slice supports X-ray and CT service points. MRI, ultrasound, OPD, laboratory, pharmacy and billing may reuse the Queue mechanics after their own stage/rule catalogs are approved.

## Durable model

- `queue.imaging_service_point` owns branch, code, name, X-ray/CT modality, optional generalized resource link, active flag and version.
- `queue.ticket` owns patient, optional Booking context, service point, opaque idempotency request, privacy-safe display token, priority, state timestamps, call count and optimistic version.
- `queue.ticket_event` is append-only transition evidence with from/to state, action, actor, reason, resulting ticket version and server time.
- `audit.event` separately records security/operation evidence without replacing domain history.

Ticket check-in uses a tenant-scoped request ID. Exact replay returns the existing ticket; reuse with different content is rejected. SQL Server execution uses a serializable transaction and request-scoped application lock. Ticket mutation, event and audit commit in one `SaveChanges` transaction.

The display token is generated from the ticket's numeric identity and contains no patient name, patient ID, Booking number, phone, email or clinical text.

## Permissions and API

| Capability | Permission |
|---|---|
| configure imaging service points | `Queues.ServicePoints.Manage` |
| check in a patient | `Queues.CheckIn` |
| view scoped worklist/reconciliation projection | `Queues.View` |
| call or recall | `Queues.Call` |
| begin preparation/start imaging/complete | `Queues.Progress` |
| cancel with reason | `Queues.Cancel` |
| assign urgent priority with reason | `Queues.Priority.Manage` in addition to check-in |
| view privacy-safe display projection | `Queues.Display.View` |

Every permission is branch-assignable. Protected tenant-scoped strings represent service-point, ticket, patient, Booking and resource IDs in Contracts and Client. A cross-tenant protected service-point ID is rejected before query execution.

The shared typed Client exposes service-point creation, check-in, worklist, display and transition calls. No host receives direct database or hub command access.

## SignalR boundary

API maps authenticated `/hubs/queue` and requires `Queues.View`. On connection, the server—not the client—derives branch groups from durable authenticated branch claims. The hub exposes no arbitrary group-subscription or mutation method.

After a successful database commit, the Application notifier invokes the API SignalR adapter. `QueueChanged` contains only:

- event type;
- protected service-point ID;
- protected ticket ID;
- status;
- version;
- occurrence time.

Clients refetch the authorized API projection after a branch event, reconnect, missed event or out-of-order event. Protected DTO identifiers use probabilistic encryption, so clients must not compare separately generated ciphertext strings to decide entity identity. The hub's server-derived branch group supplies the authorization boundary; SignalR is transient, while `queue.ticket_event` and audit remain authoritative. A supported backplane/service is still required before multi-instance production fan-out. The verified Portal implementation is recorded in [DOC-043](43-radiology-admin-portal-journey.md).

## Migration

At this slice checkpoint, generated migration `20260823100340_ImagingQueueAndRealtime` was not applied. DOC-044 subsequently records its corrected successful application to the local development database. Its reviewed `Up` path:

1. creates the `queue` schema;
2. creates service-point, ticket and append-only event tables;
3. adds tenant-aligned branch, patient, Booking, resource and service-point foreign keys;
4. adds idempotency, display, active-Booking, ordering and event-version indexes;
5. adds queue permissions to deterministic role-claim seeds.

The `Up` path contains no drop, seeded-data deletion or raw SQL. EF emitted its generic data-loss warning because deterministic permission seed rows shift; the generated operations were reviewed. The existing EF tools/runtime patch warning (`10.0.8` versus `10.0.10`) remains.

## Verification

Verified on 2026-08-23:

```text
API Release build:                         passed, 0 warnings / 0 errors
Unit tests:                                51 passed
Architecture tests:                         9 passed
Integration tests:                         32 passed
Total automated tests:                     92 passed
EF pending-model-change check:              passed (none pending)
```

Evidence covers workflow invariants, stale versions, recall count, safe display token, idempotent replay, altered replay rejection rules, urgent-priority permission, negative transition permissions, authenticated/unauthenticated hub negotiation, reconciliation reads, cross-tenant protected-ID rejection, transition/event counts and completed/cancelled terminal states.

The SQL Server smoke remains conditional. Deployed migration rehearsal, two-operator multi-connection races, SignalR reconnect/out-of-order client behavior, multi-node fan-out, accessibility and clinic acceptance remain release gates.

## Completion percentage

DOC-033 row 16 advances from **15% to 50%**. A meaningful Queue/realtime reference foundation now works through Domain, database, API, typed Client, authorization and tests, but no Admin/Portal/MAUI user journey exists.

The twenty-row score increases by 35 points in one row: from `1260 / 20 = 63%` to `1295 / 20 = 64.75%`. Therefore:

- exact architecture-achievement completion: **64.75%**;
- rounded headline completion: **65%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

No score is awarded for dashboards, radiology clinical order/protocol/safety data, PACS/DICOM, report verification/release, OPD queues, UI acceptance, push notifications or production SignalR operations.

## Recommended next goal

Status on 2026-08-23: implemented as [DOC-043](43-radiology-admin-portal-journey.md). The items below preserve this slice's original handoff.

Implement the first role-composed radiology user journey:

1. Admin service-point configuration screen under approved-network/session controls;
2. Portal radiology technician dashboard/worklist using shared Blazor components;
3. authorized call/recall/preparation/start/complete/cancel actions;
4. privacy-safe queue display component;
5. SignalR invalidation with reconnect and API version reconciliation;
6. loading, empty, error, stale-conflict and accessibility states with browser/component tests.

This is the shortest path from a backend reference foundation to an accepted user outcome and directly advances Admin, Portal, shared UI and Queue evidence without inventing the still-unapproved Contract/Package rules.
