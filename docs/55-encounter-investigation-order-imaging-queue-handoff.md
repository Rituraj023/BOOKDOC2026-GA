# 55 — Encounter Investigation Order and Imaging Queue Handoff

Owner: Clinical + Catalog/Resources + Queue + Portal + Blazor UI + security + migration + QA  
Status: Verified policy-neutral reference slice  
Reviewed: 2026-08-28

## Outcome

BOOKDOC2026 now connects a signed Encounter to the first X-ray/CT operational Queue without treating Queue progress as clinical result progress:

```text
Signed Encounter
      |
      v
Configured catalog imaging service --request--> Investigation Order: Requested
      |                                              Result: Pending
      |                                              Queue: not handed off
      v
Matching active X-ray/CT service point --handoff--> Queue Ticket: Waiting
                                                     |
                                                     v
                                      Called -> Preparation -> In Service -> Completed

Investigation Order remains Requested and Result remains Pending.
```

This slice does not implement or imply radiology interpretation, critical-result acknowledgement, result verification, ordering restrictions, protocol selection, report signing or patient release. Those remain owner-approved clinical-policy work.

## Catalog integrity

An active service is orderable as X-ray or CT only when it has a non-optional `ServiceResourceRequirement` linked to an active `ResourceCategory` whose kind is `ImagingModality` and whose canonical category code is `XRAY` or `CT` (hyphen/underscore variants normalize for lookup). The API derives the available Portal catalog from that configuration and rejects:

- an ordinary active service presented as an imaging service;
- a CT request against an X-ray service requirement, or the reverse;
- an inactive or unconfigured service;
- a request from an unsigned Encounter.

This reuses the generalized Catalog/Resource architecture instead of adding a doctor-only investigation catalog or trusting a client-supplied modality label.

## Domain and state ownership

`InvestigationOrder` owns Patient, signed Encounter, requested Service, modality, indication, idempotency identity, stable order number, request actor/time, queue-handoff evidence and optimistic version. `InvestigationOrderEvent` is append-only and records `Requested` and `QueueLinked` history without copying clinical text into operational audit.

Three states remain deliberately independent:

| State | Owner | Implemented values in this slice | What it does not mean |
|---|---|---|---|
| Order | Clinical Investigation | `Requested` | not performed, reported or clinically reviewed |
| Result | future result/report module | `Pending` | Queue completion does not complete a result |
| Queue | Queue | `Waiting`, `Called`, `Preparation`, `InService`, `Completed`, `Cancelled` | operational progression is not clinical verification |

The database check constraints intentionally allow only the current policy-neutral Order/Result baseline. Later transitions require an approved migration and state-machine evidence rather than silently reusing Queue status.

## Permissions and API

| Permission | Purpose |
|---|---|
| `Investigations.View` | list scoped orders and append-only history |
| `Investigations.Orders.Create` | list configured orderable imaging options and request an order |
| `Investigations.Queue.Handoff` plus `Queues.CheckIn` | create the first Queue ticket for an order |
| `Queues.Priority.Manage` | additionally required for an urgent handoff and reason |
| `Investigations.Worklist.View` | added by DOC-056 for the bounded technician ordered-work projection; it does not grant generic Queue visibility or commands |

Routes are typed through Contracts and Client:

- `GET /api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders/catalog-options`;
- `GET /api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders`;
- `POST /api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders`;
- `POST /api/v1/branches/{branchId}/encounters/{encounterId}/investigation-orders/{orderId}/queue-handoffs`.

Branch, Encounter, Service, Order, service-point and Queue identifiers are resource-kind- and tenant-bound protected strings. The API still rechecks tenant, durable branch scope, named permission, Encounter relationship and modality compatibility after decoding.

## Idempotency, concurrency and audit

- Order creation uses a tenant-unique request ID. Replaying identical content returns the existing order; reuse with different content is rejected.
- The human-facing `INV-...` number is a stable one-way digest of the idempotency identity and is unique within tenant/branch scope.
- Queue handoff is serialized by tenant/order, accepts one request identity, checks the expected Order version and permits only one linked Queue ticket.
- A replay returns the original token and does not publish another SignalR invalidation.
- New order, event and audit records commit atomically. Queue handoff atomically commits the Order version/handoff, Queue ticket/event and both safe audits.
- Urgent priority requires the separate priority permission and a normalized reason.
- Audit payloads contain numeric relationship IDs, modality, status and token metadata; they exclude the clinical indication.

## Persistence and migration

Reviewed migration `20260828004550_PhysiotherapyAndInvestigationClinicalDelivery` is the implementation authority. It combines the still-pending Physiotherapy delivery tables with:

- `clinical.investigation_order`;
- `clinical.investigation_order_event`;
- nullable `queue.ticket.investigation_order_id`;
- tenant-scoped foreign keys and query filters;
- unique request, order-number, one-ticket-per-order and event-version indexes;
- append-only event enforcement in the DbContext;
- append-only Identity permission seed additions.

The migration was applied to the local development SQL Server and EF reports no pending model changes. Permission seed rows were appended after existing IDs; the reviewed migration does not rewrite prior role claims.

## Portal and shared UI

The signed-Encounter Portal workspace now:

- loads only configured X-ray/CT service options allowed by the order permission;
- derives modality from the selected catalog option rather than asking the user to invent it;
- requires a bounded clinical indication;
- retains the request ID after a failed submission so retry is safe;
- renders Order, Result and Queue state separately;
- filters destinations to active branch service points with the same modality;
- retains a handoff request ID across failure and exposes urgent reason only to authorized roles;
- shows the Queue token/status and append-only Order history after handoff.

`InvestigationOrderList` and its handoff selection model live in `BookDoc2026.Blazor.UI`; API authorization and domain rules remain outside Razor. The Portal and API Release builds pass with zero warnings/errors. The public Portal shell and sign-in form rendered in the local browser; this document does not claim a new authenticated browser/UAT run.

## Development fixture

The guarded removable fixture now adds one X-ray catalog Service, one `XRAY` imaging-modality Resource Category, their required relationship and one matching X-ray service point. Its clinician role has the investigation and Queue check-in permissions.

The first removal rehearsal exposed a dependency-order defect: a service-resource requirement still referenced the category. Cleanup now removes resource status/capability/requirement rows before resources, categories and services. A repeated real SQL fixture create, corrected removal and idempotent second removal passed. No fixture starts with an application host, and no synthetic credential is stored in source or documentation.

## Verification

```text
Focused Investigation domain tests:         4 passed
Focused Encounter/Investigation API test:    1 passed
Portal Release build:                        passed, 0 warnings / 0 errors
API Release build:                           passed, 0 warnings / 0 errors
Unit tests:                                  94 passed
Architecture tests:                           9 passed
Integration tests:                           35 passed
Total automated tests:                      138 passed, 0 failed
Local SQL migration application:             passed
EF pending model changes:                    none
Fixture create/remove/second remove:          passed after dependency-order correction
```

Coverage includes unsigned Encounter rejection, configured-service and modality validation, protected options, separated permissions, idempotent create/handoff, urgent permission composition, service-point mismatch, Queue lifecycle completion without Order/Result mutation, cross-tenant denial and append-only history.

## Achievement effect

DOC-033 row 16 advances from **85% to 87.5%**: the earlier X-ray/CT Queue foundation now has a signed-Encounter order source, catalog integrity, atomic handoff and separately modeled Order/Result/Queue truth. It does not reach the 90% release-candidate threshold or advance Portal/shared-UI quality/UAT scores without the technician worklist, authenticated rendered acceptance and operational evidence.

The twenty-row score gains 2.5 points: `1502.5 / 20 = 75.125%`.

- reported architecture-achievement completion: **75.13%** (75.125% exact);
- rounded headline completion: **75%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

## Continuation

The authorized radiology ordered-work bridge is implemented in [DOC-056](56-radiology-technician-ordered-worklist.md). It preserves this document's state boundaries and adds no interpretation, findings, critical-result, verification, signing or patient-release commands.

[DOC-057](57-xray-ct-execution-result-policy-decision-pack.md) now provides the proposed X-ray/CT execution/result decision model. The next gate is named-owner approval of its applicable RAD decisions plus authenticated workflow acceptance; Queue completion must not be reused as clinical acquisition, interpretation or release evidence.
