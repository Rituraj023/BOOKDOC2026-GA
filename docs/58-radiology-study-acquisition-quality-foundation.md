# 58 — Radiology Study, Acquisition and Quality Foundation

Status: Verified backend reference slice; development/policy validation only  
Owner: Radiology + Clinical Investigations + Workforce + Resources + API + data + security + QA  
Implemented and reviewed: 2026-08-29

## Outcome

The first X-ray/CT execution slice now preserves a clinical study lifecycle independently from the Investigation Order, operational Queue and any future interpreted Result. An authorized and currently eligible operator can register and start one study for an active queued order, record immutable acquisition evidence against matching equipment, and a separately permissioned eligible reviewer can accept technical quality or require a repeat.

This is a backend reference foundation. The product owner's approval to use the recommended RAD-01 through RAD-09 defaults is recorded as provisional development authority. A Delhi clinical director, radiology lead and compliance owner must validate the policy, operator qualifications, equipment/licence/QA evidence and code sets before a tenant can use it clinically. The implementation does not create a report, interpretation, critical-result workflow, patient release or production PACS integration.

## Preserved state boundaries

```text
Investigation Order     requested clinical intent
Queue Ticket            operational patient flow
Radiology Study         performed/acquisition/quality evidence
Future Report/Result    interpretation, signing and release
```

Completing or cancelling a Queue ticket does not mutate Study, Order or Result state. `Acquired` means that an attempt was recorded; it does not mean diagnostic interpretation, final result or release.

The implemented Study state machine is:

```text
Registered -> InProgress -> Acquired -> QualityAccepted
                    |            |
                    |            +-> RepeatRequired -> InProgress
                    +----------------> Aborted
```

Registration is allowed only while the source Queue ticket and imaging service point remain active. Every command carries a request identity; versioned mutations also carry the expected Study version.

## Implemented domain and persistence

The additive `radiology` schema contains:

| Table | Responsibility and invariant |
|---|---|
| `radiology.study` | One tenant-scoped Study per Investigation Order, current execution status, attempt count and optimistic version |
| `radiology.acquisition_attempt` | Immutable sequence, operator, matching resource, protocol/version, structured deviation/outcome, protected external reference and timestamps |
| `radiology.quality_review` | Immutable one-per-attempt technical decision by an eligible reviewer with structured reason and optional bounded note |
| `radiology.study_event` | Append-only command history, request/fingerprint identity, actor and unique Study version |

Tenant-bearing foreign keys prevent cross-tenant relationships. Unique indexes protect one Study per Order, request replay identity, acquisition sequence, one quality review per attempt and optional external Study-reference uniqueness. Check constraints protect valid statuses, outcomes, sequence, timing, deviation structure and abort-reason structure. `BookDocDbContext` rejects update/delete operations for attempts, reviews and Study events.

The migration is `20260828134611_RadiologyStudyExecutionFoundation`. It was applied successfully to the local development SQL Server on 2026-08-29, and Entity Framework reports no pending model changes. The migration adds only the four radiology tables and permission seed IDs 135–142; earlier role-claim seed identities are unchanged.

## Authorization and eligibility

The slice adds four independently composable, branch-assignable permissions:

```text
Radiology.Studies.View
Radiology.Studies.Start
Radiology.Acquisitions.Record
Radiology.Studies.QualityReview
```

Permission alone is insufficient for mutations. Register, start, acquisition and quality review also require an active `Practitioner`, a current verified credential, and an effective assignment for the branch and ordered service. Quality review uses a separate permission and the performing operator cannot review their own attempt. Named roles remain configuration; the API composes permission, durable tenant/branch scope and current eligibility.

Equipment must be active, available, in the Study's branch, categorized with the matching X-ray/CT imaging modality and advertise the ordered service capability. If the Queue service point is bound to a specific resource, acquisition must use that resource. Equipment compliance evidence is not yet implemented, so the tenant live-use gate remains closed.

## API and public identity

The explicit v1 endpoints are:

```text
GET  /api/v1/branches/{branchId}/investigation-orders/{orderId}/radiology-study
POST /api/v1/branches/{branchId}/investigation-orders/{orderId}/radiology-study
GET  /api/v1/branches/{branchId}/radiology/studies/{studyId}
POST /api/v1/branches/{branchId}/radiology/studies/{studyId}/start
POST /api/v1/branches/{branchId}/radiology/studies/{studyId}/acquisitions
POST /api/v1/branches/{branchId}/radiology/studies/{studyId}/quality-reviews
```

Internal keys remain positive signed numeric values. Contracts expose type- and tenant-bound protected strings for Study, event, attempt and quality-review identity. The raw external PACS/VNA reference is retained internally but never returned; the response exposes only `HasExternalStudyReference`.

## Replay, concurrency and privacy behavior

- A repeated request ID with the same action and canonical fingerprint returns the current aggregate with `IsReplay = true` and cannot duplicate a Study, exposure attempt or review.
- Reuse of a request ID with changed content or another aggregate fails closed.
- Expected versions reject stale concurrent mutations; serialized SQL transactions and application locks close first-write races.
- Study events and technical evidence are append-only; a repeat preserves the rejected attempt before a new cycle starts.
- Audit metadata records protected operational facts only. It excludes clinical narrative, deviation/outcome notes and raw external Study references.
- SignalR is not part of the durable command and no Queue transition fabricates a Study transition.

## Verified evidence

Focused automated evidence proves:

- register/start/acquire/review state transitions and idempotent replay;
- a repeat cycle preserves prior attempts;
- invalid timing, uncoded deviations and uncoded aborts fail;
- worklist permission cannot register or view a Study;
- mutation permission without practitioner eligibility fails;
- CT equipment is rejected for an X-ray Study even when it advertises the service;
- a performing operator cannot self-review;
- an ineligible reviewer fails and a separately eligible reviewer succeeds;
- raw external identifiers never appear in API JSON;
- Queue completion leaves a quality-accepted Study unchanged;
- cross-tenant protected Study identity fails closed.

At this backend milestone the full automated baseline was 141 tests: 97 unit, 9 architecture and 35 integration. The subsequent Portal/browser slice in [DOC-059](59-portal-radiology-execution-quality-workspace.md) raises the current verified baseline to 144 tests: 100 unit, 9 architecture and 35 integration.

## Deliberately deferred

- Admin/MAUI Study execution screens; the shared Portal operator/reviewer screen and authenticated browser acceptance are completed in DOC-059;
- protocol catalog ownership UI and approved deviation/abort reason catalogs;
- equipment licence, regulatory and QA-evidence source/gating;
- PACS/RIS/DICOM modality worklist integration and reconciliation;
- report draft/version/sign/amend, criticality, communication and acknowledgement;
- clinical or patient release, immutable report snapshot and notification delivery;
- production enablement, owner UAT, privacy review and operational runbook.

RAD-10 through RAD-24 remain pending. No report/result endpoint, table, permission or UI has been added.

## Score effect

DOC-033 row 16 advances conservatively from **88.75% to 89.5%** because the durable Study/acquisition/quality backend is implemented with authorization, isolation and migration evidence. It remains below the 90% release-candidate threshold because clinical/radiology/compliance validation, role-facing browser acceptance, realtime reconciliation, multi-node operation and production enablement remain open.

The twenty-row score gains 0.75 points: `1504.5 / 20 = 75.225%`, reported as **75.23%**.

## Recommended next goal at this milestone — completed in DOC-059

Build a shared Blazor/Portal Slice-A workspace on the existing ordered-work worklist: one eligible operator session registers, starts and records acquisition; a distinct eligible reviewer session accepts or requests repeat; both reconcile from the API after every command. Add development fixtures and authenticated browser evidence for permission denial, self-review denial, wrong-modality equipment, idempotent retry and Queue/Study independence. Keep report signing, critical communication and patient release absent, and retain the tenant live-use gate until the named clinical owners validate RAD-01 through RAD-09.

DOC-059 completes that role-facing engineering goal and records the next recommendation: privacy-safe Study-status SignalR invalidation/reconciliation plus reconnect evidence. The clinical owner and tenant live-use gates remain unchanged.
