# 56 — Radiology Technician Ordered-Work Worklist

Owner: Clinical Investigations + Queue + Portal + Blazor UI + Identity + migration + QA  
Status: Verified policy-neutral reference slice  
Reviewed: 2026-08-28

## Outcome

BOOKDOC2026 now gives an authorized X-ray/CT technician the minimum clinical context needed to act on a Queue ticket created from an Investigation Order:

```text
Signed Encounter -> Investigation Order -> matching Queue handoff
                                                |
                                                v
                              permission-scoped technician worklist
                              - token and Queue status
                              - Patient name/number reference
                              - Encounter number reference
                              - configured Service and modality
                              - clinical indication
                                                |
                                                v
                              existing Queue command permissions
                              Call -> Preparation -> In Service -> Complete
```

The worklist does not contain Encounter note bodies, diagnoses/findings, result content, interpretation, clinical documents, patient contact details, report/signing controls or release controls. Queue completion still does not change the Investigation Order from `Requested` or its Result from `Pending`.

## Authorization boundary

The new branch-assignable permission is `Investigations.Worklist.View`. It is intentionally separate from:

- `Queues.View`, which continues to protect the generic Queue-ticket projection;
- `Investigations.View`, which protects clinician-facing Order history;
- `Queues.Call`, `Queues.Progress` and `Queues.Cancel`, which continue to protect individual operational commands;
- `Queues.Display.View`, which protects the privacy-safe public token/status display.

`Queues.View` alone cannot open ordered clinical context. Conversely, `Investigations.Worklist.View` can discover authorized branch imaging service points and load the ordered-work projection, but it does not grant the generic Queue list or any transition command. The Portal navigation and dashboard card use the new permission rather than a role-name check.

The development technician role now models this least-privilege composition: ordered-work view plus the independently selected Queue command/display permissions; it no longer needs `Queues.View` for the Portal technician journey.

## API and bounded projection

The typed route is:

`GET /api/v1/branches/{branchId}/investigations/worklist?servicePointId={protectedId}&take={1..200}`

The Application layer rechecks authenticated tenant, durable branch assignment, named permission, worklist bound and service-point membership. Infrastructure returns only active linked work—Queue tickets other than `Completed` or `Cancelled` that have an Investigation Order—and joins:

- Queue ticket to the exact Investigation Order;
- Order to the configured Catalog Service;
- Order/Queue Patient identity to the Patient and central Stakeholder display name;
- Order to the same-branch Encounter reference;
- Order modality to the selected imaging service-point modality.

The response contains protected Patient, Encounter, Service, Order, Queue-ticket and service-point identifiers. Public protected strings are never compared as stable identities. The query is bounded to 200 items, prioritizes urgent work and then orders by arrival time. It does not load or serialize Encounter revisions.

## Portal and shared UI

The Portal Radiology page now consumes the ordered-work endpoint instead of the generic token-only Queue endpoint. The shared `RadiologyWorklist` component renders:

- token, arrival, priority and authoritative Queue status;
- bounded Patient name/number;
- configured service, Order number and modality;
- an explicit `Open context` disclosure for indication, Encounter number, Order time, service code and call count;
- only those Queue buttons allowed by the actor's separate command permissions;
- a visible statement of the intentionally excluded clinical/result/document boundary.

SignalR remains an invalidation channel. A scoped Queue event causes the Portal to reload the worklist from the API; it never injects Patient/Order context into the live event and never performs a durable transition itself. Completed/cancelled work disappears from this active projection after authoritative refresh.

## Migration

Migration `20260828063245_RadiologyTechnicianWorklistPermission` appends only two Identity seed rows:

| Claim ID | Seed role | Permission |
|---:|---|---|
| 133 | platform administrator | `Investigations.Worklist.View` |
| 134 | branch administrator | `Investigations.Worklist.View` |

The worklist is a module-owned query over existing normalized tables, so it needs no duplicate worklist table, result table or clinical-text copy. The migration does not update or renumber earlier role claims. It was applied successfully to the local development SQL database and EF reports no model drift.

## Verification

```text
Focused worklist projection tests:             2 passed
Focused Encounter/Investigation API test:      1 passed
Portal Release build:                          passed, 0 warnings / 0 errors
API Release build:                             passed, 0 warnings / 0 errors
Unit tests:                                    94 passed
Architecture tests:                             9 passed
Integration tests:                             35 passed
Total automated tests:                        138 passed, 0 failed
Local SQL permission migration:                passed
```

The expanded API integration proves:

- `Queues.View` without the worklist permission is denied;
- worklist-only access can discover branch service points but cannot read the generic Queue list;
- only the linked active Investigation Order appears;
- Patient and Encounter IDs remain protected;
- the expected Patient/Encounter/Service/Order/indication references are present;
- Encounter history/findings and Result/history properties are absent from serialized output;
- Queue completion removes the item from the active worklist;
- a protected service-point ID cannot cross tenant boundaries.

The public Portal/session-required state is suitable for rendered smoke verification. This checkpoint does not claim a new authenticated clinician-to-technician browser run, clinic UAT, broad accessibility acceptance or production SignalR/scale evidence.

## Achievement effect

DOC-033 row 16 advances from **87.5% to 88.75%**. The missing technician Order-context bridge is implemented with least-privilege and data-minimization evidence, but the row remains below the 90% release-candidate threshold because authenticated clinician-to-technician browser acceptance, reconnect/multi-node operations, OPD/other department coverage and owner-accepted workflow evidence remain open.

The twenty-row score gains 1.25 points: `1503.75 / 20 = 75.1875%`.

- reported architecture-achievement completion: **75.19%**;
- rounded headline completion: **75%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

## Policy-pack continuation

[DOC-057](57-xray-ct-execution-result-policy-decision-pack.md) completes the specialty policy structure for X-ray/CT execution and result ownership: performed/acquired/quality separation, operator and equipment eligibility, protocol/deviation evidence, interpreter assignment, preliminary/final/amended versions, critical-result acknowledgement, report signing, clinical/patient release and immutable document snapshots.

The product owner provisionally accepted the recommended RAD-01 through RAD-09 development defaults, and [DOC-058](58-radiology-study-acquisition-quality-foundation.md) now implements the bounded Study/acquisition/quality backend with audit, authorization, replay, immutable history and tenant-isolation evidence. Clinical/radiology/compliance validation remains mandatory before tenant live use. RAD-10 through RAD-24 remain pending, and Queue completion still cannot infer interpretation or result release.
