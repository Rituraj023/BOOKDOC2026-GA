# 06 — Delivery Roadmap and Effort

## Estimation basis

These are engineering-effort ranges for one experienced full-stack .NET developer using AI assistance, with timely domain decisions. They include implementation, normal review, automated tests, and technical documentation; they exclude waiting time, compliance/legal consulting, hardware, vendor onboarding, app-store review, production data cleanup performed by business staff, and full hospital modules.

Expect elapsed time to be longer than effort hours. Re-estimate after production database discovery and the first complete vertical slice. A 25–40% uncertainty reserve is appropriate now.

## Clinic MVP work breakdown

| Epic | Scope | Estimate (hours) | Depends on |
|---|---|---:|---|
| E0 Discovery and decisions | live schema/profile, workflow/report/integration catalog, terminology, compliance inputs | 60–100 | stakeholder/data access |
| E1 Solution foundation | scaffold, CI, architecture tests, API conventions, Aspire, environments, secrets | 70–110 | E0 key decisions |
| E2 Security and platform | identity, activation/MFA policy, scopes, permissions, audit, outbox, jobs, documents | 110–170 | E1 |
| E3 Organization and catalogs | clinic/branch/room, numbering/settings, specialties, services, price lists | 80–130 | E1–E2 |
| E4 Patient Registry | patient/contact/relation/consent/alerts, search, duplicate/merge, documents | 150–230 | E2–E3 |
| E5 Workforce and scheduling | practitioners, availability, slots/holds, booking, waitlist, queue, notifications | 210–320 | E2–E4 |
| E6 Clinical encounter | encounter lifecycle, notes, vitals, diagnoses, procedures, templates, print | 220–340 | E4–E5 |
| E7 Prescription and basic investigations | structured prescription, issue/amend, orders/basic results | 130–210 | E6, catalogs |
| E8 Clinic billing | invoices, pricing/tax snapshots, payments, allocations, refunds, daily close | 190–290 | E3–E6 |
| E9 Web experiences | Admin, Staff Portal and patient self-service polish beyond vertical slices | 150–240 | E2–E8 incrementally |
| E10 Core report read models | operational dashboards, clinical summaries and initial billing/appointment projections used by vertical slices | 110–190 | data modules |
| E11 Migration tooling and rehearsals | profiler, maps, ETL, quarantine, reconciliation, two+ rehearsals | 220–380 | E0 and module schemas |
| E12 Hardening and pilot | load/security/accessibility, observability, backup/restore, runbooks, pilot fixes | 150–240 | all MVP |
| E13 MAUI stage 1 | shared client, secure auth, patient booking/appointments/notifications/docs | 180–280 | stable E2/E4/E5 APIs |
| E14 MAUI staff stage 2 | scoped agenda, patient lookup/timeline, encounter draft/vitals, tasks | 220–360 | stable E2/E4–E7 APIs |
| E15 MAUI front desk/cashier stage 3 | check-in/queue, staff booking, selected collections | 120–200 | stable E5/E8 APIs and mobile controls |
| E16 Multi-role dashboards | permission-composed dashboards/cards/actions in both Admin and Portal | 120–200 | E2 plus module read models |
| E17 General resource scheduling | categories, rooms/beds/modalities/equipment/teams, multi-resource availability and conflict rules | 180–300 | E3–E5 |
| E18 Queue and radiology foundation | service points, tickets/stages/SLA, X-ray/CT/MRI worklist and report-status foundation | 220–360 | E4–E7, E17 |
| E19 Realtime and channel expansion | SignalR groups/recovery, push installations, template registry, email refresh, WhatsApp provider/webhooks | 160–260 | E2, platform outbox/messaging |
| E20 Worker library and Windows-service retirement | API-hosted typed handlers, commands/monitoring, retry/dead-letter/idempotency and controlled parallel run | 100–160 | E2, E19, legacy task inventory |
| E21 Reporting platform and access | Admin catalog/schedules/monitoring, Portal operational catalog, HTML/PDF/XLSX/CSV, snapshots and permissions | 180–300 | E2, E10, stable module projections |
| E22 Active legacy report reconstruction | classify 45 primary RDLC layouts and 43 export variants; rebuild/merge/replace accepted active set with golden results | 360–675 | E21, owners, legacy procedures/data |
| E23 Selective local printing | branch-bound agent registration, receipts/queue slips/labels, claims/acknowledgements, offline and duplicate safety | 140–240 | E2, E21, printer/hardware discovery |

Clinic web MVP excluding MAUI: roughly **1,850–2,950 hours** before uncertainty reserve.  
With MAUI stage 1: roughly **2,030–3,230 hours** before uncertainty reserve.

If the validated Xamarin staff journeys are also required, add approximately **340–560 hours** for stages M2/M3. This is additive to the clinic web MVP and patient M1 estimate; it should be refined after runtime walkthroughs of both old apps.

The newly confirmed dashboards, generalized resource booking, departmental queues/radiology foundation and realtime/multichannel communication add approximately **680–1,120 hours**. Before the Worker/reporting/printing work, the expanded clinic web plan is therefore roughly **2,530–4,070 hours** before uncertainty reserve; with patient MAUI M1 it is roughly **2,710–4,350 hours**.

The API-managed Worker replacement, reporting platform, active legacy report reconstruction and selective local printing add approximately **780–1,375 hours**. The consolidated expanded clinic web plan is therefore **3,310–5,445 hours before uncertainty reserve**. With patient MAUI M1 it is **3,490–5,725 hours**. Staff MAUI M2/M3 remains an additional **340–560 hours**. These ranges are planning allowances, not authorization to rebuild every legacy report: the 45 primary layouts and 43 export variants must first be classified, and E22 is recalibrated from the accepted active set.

The cross-cutting controls in [DOC-034](34-cross-cutting-platform-hardening.md) refine work already budgeted across E1 (foundation), E2 (security/platform), integration epics and E12 (hardening/pilot). They do not add a separate epic or change the consolidated ROM until a measured cache/search product, additional infrastructure or new compliance requirement is approved. Re-estimation must prevent the same telemetry, audit, resilience or recovery work from being counted in both a module and E12.

[DOC-035](35-path-to-100-percent-achievement.md) sequences these existing epics against the twenty-row achievement report. Its current reported 70.50% baseline measures evidence maturity, not consumed engineering hours, so it must not be subtracted from this ROM.

Employee/payroll work is not included because its database and authoritative business rules have not yet been supplied. It will receive a separate discovery, mapping and estimate.

This is a production-oriented estimate. A demo with happy paths can be much smaller, but should not be called a safe clinic-management release.

## Suggested release increments

### R0 — Architecture reference slice

- Foundation, one scope/permission, one catalog entity, migration, typed client, Admin page, tests.
- Goal: prove the reusable delivery path before many entities exist.
- Complete Xamarin screen disposition (`preserve/combine/replace/web-only/defer/retire`) before mobile backlog commitment.

### R1 — Front desk core

- Organization/catalog/resources, Patient Registry, practitioner setup, generalized schedule/booking, request, check-in and basic queue.
- Migration waves 0–5 rehearsal.
- Can support controlled front-desk user acceptance with clinical/billing still legacy.

### R2 — Clinic care

- Encounter, notes/vitals/diagnosis/procedure, prescription, basic investigations, clinical summary.
- Department queue foundation and first X-ray/CT modality worklist vertical slice.
- Migration wave 6 and clinical safety acceptance.

### R3 — Revenue and reports

- Invoice/payment/refund/close, appointment and financial reports, full reconciliation.
- Permission-composed Admin/Portal dashboards, SignalR live invalidation and management metrics.
- Establish the report engine and permissions: Admin owns catalog, schedules and monitoring; Portal exposes only permission-approved, role-scoped operational reports.
- Migration wave 7, operational parallel run.

### R4 — Pilot and patient mobile

- Hardening, dress rehearsal/cutover, MAUI patient journeys, push, updated email templates, WhatsApp channel, production monitoring/support.
- Run the replacement Worker-library handlers inside the production API after channel/task-family parallel-run acceptance; retire the Windows service only after the observation gate.
- Complete active-report golden comparisons and approved RDLC/WinForms retirement decisions; ordinary output uses HTML preview, PDF, XLSX/CSV and browser printing.

### R5 — Clinic optimization

- Specialty templates, deeper integrations, advanced dashboards, staff mobile workflows selected by evidence.
- Introduce the local print agent only where receipts, queue slips or labels cannot meet accepted operational needs through browser/PDF printing.

### R6 — Hospital foundation

- Separate discovery and estimates for inpatient/nursing/lab/pharmacy/radiology/OT/emergency/insurance.

## First 160 engineering hours

| Hours | Outcome |
|---:|---|
| 0–24 | discovery scripts/runbook, source inventory, terminology/decision workshop inputs |
| 24–48 | target solution skeleton, dependency/architecture tests, CI baseline |
| 48–80 | scope/auth/permission reference behavior and secure configuration |
| 80–112 | audit/outbox/platform registration and first database migration |
| 112–144 | Organization/Branch reference vertical slice through API/client/Admin |
| 144–160 | integration/authorization tests, documentation, estimate recalibration |

Do not spend the first hours mass-generating every entity. The first slice exists to expose architecture and operational problems cheaply.

## Worker, reporting and printing phase gates

| Gate | Required evidence | Dependency |
|---|---|---|
| W0 inventory | every active legacy message/repeat task has owner, typed-handler disposition and delivery risk | live service/database discovery |
| W1 shadow/parallel | suppressed shadow comparison followed by mutually exclusive channel/task ownership and reconciled outcomes | E20 handlers and monitoring |
| W2 retirement | API-hosted Worker subsystem stable through observation period; rollback and owner/operations approval recorded | W1 |
| RP0 classify | all 45 primary RDLC layouts and 43 export variants have owner, usage evidence and `Rebuild`/`Merge`/`Replace`/`Retire` disposition | report users and legacy evidence |
| RP1 platform | Admin/Portal ownership, three-part authorization, HTML/PDF/XLSX/CSV outputs and snapshot rules pass tests | E21 |
| RP2 parity/retirement | active reports pass golden comparisons; intentional differences and unused-report retirements are approved | RP0–RP1, E22 |
| P0 browser first | normal reports, documents and forms meet accepted browser/PDF print behavior | RP1 |
| P1 local agent | only receipts, queue slips and labels pass authorization, branch binding, offline, duplicate and wrong-printer tests | P0, E23, hardware discovery |

Detailed scope and acceptance criteria are maintained in [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).

## Six-hour implementation sessions

The request for “hour addition and help” is represented as small, reviewable six-hour work sessions. Each session must end with code/tests/docs committed or a documented decision/blocker.

Typical session:

1. 0.5h — select one acceptance outcome, review dependencies and current tests.
2. 1.0h — design contract/invariant/database change and enumerate failure cases.
3. 2.5h — implement the smallest complete vertical behavior.
4. 1.25h — unit/integration/authorization tests and migration verification.
5. 0.5h — manual smoke/accessibility check.
6. 0.25h — update master/backlog/decision and handoff notes.

Good six-hour work items:

- one permission and authorization matrix;
- one value object plus domain tests;
- one migration/profile rule and reconciliation test;
- one typed query endpoint/client/UI list;
- one command state transition plus concurrency tests;
- one MAUI screen using existing stable APIs.

Poor six-hour items are “finish scheduling,” “create database,” or “build mobile app”; split them by observable outcome.

## Backlog item template

```text
ID / title:
Module / release:
User and outcome:
Preconditions:
Business invariants and state transition:
Permission and scope:
API contract / errors / idempotency:
Database change / indexes / concurrency:
Audit, privacy and side effects:
UI states and accessibility:
Tests and reconciliation:
Migration/backward compatibility:
Definition of done:
Estimate / dependencies / owner:
```

## Definition of done

A feature is done only when:

- invariant and error behavior are explicit;
- permissions and tenant/branch isolation are tested;
- API, typed client and relevant UI are complete;
- migration and indexes are reviewed and tested;
- concurrency/idempotency are addressed;
- audit/privacy/logging are addressed;
- unit, integration, API and relevant UI tests pass;
- observability and operational support behavior exists;
- docs/registers are updated and product acceptance evidence is recorded.
- affected Worker tasks, report golden results, snapshot rules and printer-agent scenarios meet their phase gates.

## Resourcing guidance

Parallel work is safest after E1/E2 and the reference slice establish conventions. Natural streams are Platform/Security, Patient/Scheduling, Clinical, Billing/Migration, and UI/Mobile/QA. Each data-owning module still needs a named owner to prevent cross-module table coupling.

## Hospital estimate policy

Do not attach a single number to “hospital management.” Run a separate discovery for each module. Inpatient, nursing/MAR, lab, pharmacy/stock, radiology/PACS, OT, emergency and insurance each contain safety-critical workflows and integrations that can individually approach the size of the clinic MVP.
