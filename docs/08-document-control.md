# 08 — Document Control and Working Templates

## Purpose

Keep planning and generated implementation artifacts discoverable, current, and traceable. The root [Implementation Master](../README.md) is the only top-level navigator; this file controls all supporting artifacts.

## Current document register

| ID | Document | Owner | Status | Update trigger |
|---|---|---|---|---|
| DOC-000 | [Implementation Master](../README.md) | Product + architecture | Baseline | phase/scope/status change |
| DOC-001 | [Discovery Baseline](01-discovery-baseline.md) | Discovery lead | Needs live DB confirmation | new source evidence |
| DOC-002 | [Legacy Migration Assessment](02-legacy-migration-assessment.md) | Product + migration | Draft for approval | workflow/data disposition change |
| DOC-003 | [Target Architecture and New Logic](03-target-architecture-and-new-logic.md) | Architecture lead | Proposed | accepted ADR or new module |
| DOC-004 | [Database Design](04-database-design-step-by-step.md) | Data/module owners | Conceptual | schema/profile/migration change |
| DOC-005 | [Migration and Test Plan](05-migration-and-test-plan.md) | Migration + QA | Proposed | rehearsal/test-gate result |
| DOC-006 | [Delivery Roadmap and Effort](06-delivery-roadmap-and-effort.md) | Delivery lead | ROM estimate | discovery/slice/release change |
| DOC-007 | [MAUI Mobile Plan](07-maui-mobile-plan.md) | Mobile lead | Proposed | persona/offline/API decision |
| DOC-008 | This document | Documentation owner | Active | any artifact added/retired |
| DOC-009 | [Legacy Xamarin Mobile Discovery](09-xamarin-mobile-discovery.md) | Mobile + discovery lead | Needs runtime validation | Xamarin workflow/security evidence changes |
| DOC-010 | [Role Dashboards, Resource Booking, Queues and Realtime Messaging](10-role-dashboards-resource-queue-realtime-messaging.md) | Product + architecture | Approved planning direction | dashboard/resource/queue/channel requirements change |
| DOC-011 | [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md) | Architecture + reporting + operations | Worker foundation implemented; reporting/printing proposed | Worker task, report disposition, delivery or printing decision changes |
| DOC-012 | [Product Vision, Scope and Tenancy](12-product-vision-scope-and-tenancy.md) | Product + architecture | Draft for approval | market, tenancy, scope or success-measure change |
| DOC-013 | [Stakeholders, Roles and Permissions](13-stakeholders-roles-and-permissions.md) | Product + security | Draft for approval | persona, host, scope or permission decision |
| DOC-014 | [Workflow and Use-Case Catalog](14-workflow-and-use-case-catalog.md) | Product + clinic owners | Draft for approval | workflow, actor or exception discovery |
| DOC-015 | [Business Rules and State Machines](15-business-rules-and-state-machines.md) | Product + module owners | Proposed | invariant, transition or approval-rule change |
| DOC-016 | [Functional Requirements and Acceptance](16-functional-requirements-and-acceptance.md) | Product + QA | Active template/baseline | requirement, release or acceptance change |
| DOC-017 | [Nonfunctional, India Compliance and Security](17-nonfunctional-india-compliance-and-security.md) | Security + privacy + operations | Needs legal/state confirmation | law, state, risk, service-level or retention decision |
| DOC-018 | [Terminology and Data Ownership](18-terminology-and-data-ownership.md) | Architecture + domain owners | Draft for approval | vocabulary or system-of-record decision |
| DOC-019 | [UI/UX Navigation and Screen Inventory](19-ui-ux-navigation-and-screen-inventory.md) | UX + product | Proposed | role journey, navigation or design-system decision |
| DOC-020 | [Integrations and Vendor Readiness](20-integrations-and-vendor-readiness.md) | Architecture + procurement + security | Needs vendor decisions | provider, contract, interface or rollout change |
| DOC-021 | [Deployment and Operational Readiness](21-deployment-and-operational-readiness.md) | Platform + operations | Proposed | environment, SLO, recovery or release decision |
| DOC-022 | [Implementation Readiness Checklist](22-implementation-readiness-checklist.md) | Architecture + delivery | Active gate | module enters or leaves ready state |
| DOC-023 | [Pre-development Decisions and Questionnaire](23-pre-development-decisions-and-questionnaire.md) | Product owner + discovery | Active | answer, assumption, decision or blocker changes |
| DOC-024 | [Physiotherapy and Orthopaedics Preparation](24-physiotherapy-orthopaedics-preparation.md) | Clinical product owners + specialty representatives | Draft for approval | specialty workflow, template, permission or safety decision |
| DOC-025 | [Pre-development Architecture and Workflow Diagrams](25-predevelopment-diagrams.md) | Product + architecture | Planning baseline | tenancy, onboarding, Worker or gate decision changes |
| DOC-026 | [Foundation Reference Slice](26-foundation-reference-slice.md) | Architecture + backend | In implementation | implementation, verification or acceptance-gate change |
| DOC-027 | [Patient Registry Reference Slice](27-patient-registry-reference-slice.md) | Patient Registry + backend | In implementation | patient contract, schema, verification or readiness change |
| DOC-028 | [Resource Catalog Reference Slice](28-resource-catalog-reference-slice.md) | Catalog/Resources + backend | In implementation | service/resource contract, schema, capability or readiness change |
| DOC-029 | [Stakeholder Party Reference Slice](29-stakeholder-party-reference-slice.md) | Stakeholder + Patient Registry + backend | Verified reference slice | party ownership, document or patient-linkage change |
| DOC-030 | [Scheduling Foundation Reference Slice](30-scheduling-foundation-reference-slice.md) | Scheduling + Resources + backend | Verified reference slice | availability, hold, reservation or concurrency change |
| DOC-031 | [Numeric and Protected Identifier Reference](31-numeric-and-protected-identifier-reference.md) | Architecture + security + database + API | Implemented reference decision | key type, public contract, node allocation or Data Protection change |
| DOC-032 | [SDTS Architecture Realignment and Legacy Business Preservation](32-sdts-architecture-realignment-and-legacy-business-preservation.md) | Architecture + identity + Scheduling + Billing | Architecture implemented; business specification approved | host/Identity boundary or contract/booking/payment rule changes |
| DOC-033 | [Architecture Understanding and Achievement Report](33-architecture-understanding-and-achievement-report.md) | Product + architecture + delivery | Evidence-based checkpoint | architecture boundary, implementation evidence or readiness score changes |
| DOC-034 | [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md) | Architecture + security + operations + module owners | Proposed architecture baseline | observability, configuration, compatibility, resilience, storage, rollout or continuity decision changes |
| DOC-035 | [Path to 100% Architecture and Clinic-MVP Achievement](35-path-to-100-percent-achievement.md) | Product + architecture + delivery + security + operations + module owners | Proposed execution control plan | DOC-033 score, evidence, scope, gate, dependency or completion decision changes |
| DOC-036 | [Durable Messaging Reference Slice](36-durable-messaging-reference-slice.md) | Communications + backend + Worker + security + operations | Verified backend reference slice | template, provider, outbox, callback, consent, delivery-status or Worker evidence changes |
| DOC-037 | [Admin Communication Management Reference Slice](37-admin-communication-management-reference-slice.md) | Admin + Communications + security + backend | Verified reference slice | Admin template, override, preview, authorization, audit or monitoring evidence changes |
| DOC-038 | [Communication Preferences and Callback Inbox Reference Slice](38-communication-preferences-and-callback-inbox-reference-slice.md) | Communications + Stakeholder + Worker + Admin + security | Verified reference slice | preference, consent, callback signature, inbox, reconciliation or provider evidence changes |
| DOC-039 | [Admin Security Foundation Reference Slice](39-admin-security-foundation-reference-slice.md) | Admin + Identity + security + operations | Verified reference slice | Admin session, permission navigation, approved-network or trusted-proxy evidence changes |
| DOC-040 | [Generalized Booking and Preference-Controlled Notification Reference Slice](40-generalized-booking-and-preference-notification-reference-slice.md) | Scheduling + Resources + Stakeholder + Communications + Worker + security | Verified backend reference slice | Booking confirmation, allocation, requirement-role, preference-dispatch or Booking-notification evidence changes |
| DOC-041 | [Booking Lifecycle and Waitlist Reference Slice](41-booking-lifecycle-and-waitlist-reference-slice.md) | Scheduling + Resources + Stakeholder + Communications + Worker + security | Verified backend reference slice | cancellation, reschedule, lineage, capacity-release, waitlist or lifecycle-notification evidence changes |
| DOC-042 | [Imaging Queue and Realtime Reference Slice](42-imaging-queue-and-realtime-reference-slice.md) | Queue + Radiology + API + Portal foundation + security | Verified backend/realtime reference slice | service-point, ticket, transition, display, SignalR scope or queue evidence changes |
| DOC-043 | [Radiology Admin and Portal Journey](43-radiology-admin-portal-journey.md) | Admin + Portal + shared UI + Queue + security | Verified web reference journey | radiology setup/worklist/display, Portal session, shared component or realtime-reconciliation behavior changes |
| DOC-044 | [Permission Dashboards and Radiology Reception](44-permission-dashboards-and-radiology-reception.md) | Admin + Portal + shared UI + Patient + Queue + migration | Verified reception/dashboard reference slice | dashboard composition, patient search/check-in, retry identity, catalog-use or local migration evidence changes |
| DOC-045 | [Development Fixtures and Browser Acceptance](45-development-fixtures-and-browser-acceptance.md) | Portal + shared UI + Queue + Identity + QA | Verified local browser reference journey | fixture safety, Portal session, rendered shared component, SignalR browser or accessibility evidence changes |
| DOC-046 | [Contract and Package Entitlement Reference Slice](46-contract-package-entitlement-reference-slice.md) | Contract + Scheduling + backend + security + migration | Verified policy-neutral reference slice | entitlement policy, Booking coupling, Contract API/schema, migration or Billing boundary changes |
| DOC-047 | [Encounter Revision and Signing Foundation](47-encounter-revision-signing-foundation.md) | Clinical + backend + security + migration | Verified policy-neutral reference slice | signing/amendment policy, specialty fields, practitioner verification, clinical API/schema or document snapshot changes |
| DOC-048 | [Practitioner Credential and Assignment Foundation](48-practitioner-credential-and-assignment-foundation.md) | Workforce + Clinical + Identity + Resources + security + migration | Verified policy-neutral backend reference slice | credential, assignment, practitioner-resource, signer eligibility, workforce UI or Employee/Payroll boundary changes |
| DOC-049 | [Billing Invoice, Payment and Receipt Foundation](49-billing-invoice-payment-receipt-foundation.md) | Billing + Patient + Scheduling + Contracts + security + migration | Verified policy-neutral backend reference slice | price/tax/discount, payment, allocation, snapshot, refund/reversal, close, gateway or billing UI changes |
| DOC-050 | [Portal Cashier and Admin Billing Oversight](50-portal-cashier-and-admin-billing-oversight.md) | Billing + Portal + Admin + Blazor UI + Client + security | Verified role-facing reference slice | cashier workflow, Billing list/query, shared finance UI, printing, Admin oversight or finance-policy changes |

Status vocabulary: `Proposed`, `Draft for approval`, `Approved`, `In implementation`, `Verified`, `Superseded`, `Retired`.

## Required future artifact tree

Create files only when work begins; register each here and link it from the owning plan.

```text
docs/
  adr/                         architecture decision records
  discovery/                   schema profile, workflow and report catalogs (no PHI)
  architecture/                module context/dependency/API conventions
  database/                    ERDs, data dictionary, reviewed migration notes
  migration/                   mapping specs, rehearsal reports, cutover runbooks
  testing/                     test strategy, matrices, traceability, release evidence
  operations/                  deployment, backup/restore, monitoring, incident runbooks
  security-privacy/            classification, threat models, retention/access policies
  mobile/                      device support, offline matrix, release checklist
  releases/<version>/          scope, known issues, go/no-go and rollback evidence
```

Sensitive production data, credentials, access tokens, patient text, database backups, raw extracts, and unredacted logs must not be stored in this documentation tree.

## Naming rules

- Plans: `NN-topic.md` when order matters.
- ADRs: `ADR-NNN-short-title.md`.
- Rehearsals: `YYYY-MM-DD-rehearsal-N-summary.md`.
- Releases: `releases/vX.Y/` with immutable final evidence.
- Use repository-relative links inside documents so navigation works locally and in Git hosting.
- Each document starts with owner/status/last-reviewed when it becomes operational.

## Architecture Decision Record template

```markdown
# ADR-NNN — Decision title

Status: Proposed | Accepted | Superseded  
Date: YYYY-MM-DD  
Owners: names/roles

## Context
What problem and constraints require a decision?

## Decision
What exactly will be done?

## Alternatives considered
What was rejected and why?

## Consequences
Benefits, costs, risks, migration and operational effects.

## Evidence and validation
Prototype/tests/measurements/approvals.

## Links
Affected modules, backlog items, migrations, API contracts and documents.
```

## Decision log seed

Create ADRs before implementation for:

| ADR | Decision required | Proposed default |
|---|---|---|
| ADR-001 | Product/module architecture | .NET 10 modular monolith based on SDTS pattern |
| ADR-002 | Tenancy and branch model | tenant + organization + branch, durable user grants |
| ADR-003 | Database/schema/naming | SQL Server, schema-per-module, EF Core reviewed migrations |
| ADR-004 | Identifier strategy | internal key plus scoped human number and legacy map |
| ADR-005 | Time and time zone | UTC instants + branch/source time-zone ID |
| ADR-006 | Clinical signing/amendment | immutable signed versions |
| ADR-007 | Scheduling concurrency | transactional occupancy strategy with concurrency tests |
| ADR-008 | Documents/storage/scanning | SDTS abstraction + shared production store + quarantine |
| ADR-009 | Authentication/mobile session | short access, rotated refresh, secure device storage/revocation |
| ADR-010 | Migration/cutover | repeatable staged ETL, reconciliation, rehearsed rollback bridge |
| ADR-011 | Durable backend execution | API is the only host; `BookDoc2026.Worker` is the immediate/scheduled job library |
| ADR-012 | Reporting engine and ownership | Admin management/scheduling; permission-approved Portal operations; HTML/PDF/XLSX/CSV |
| ADR-013 | Report retention | regenerate operational reports; immutable snapshots for issued/official records |
| ADR-014 | Local printing | browser/PDF by default; constrained branch-bound agent for receipts, queue slips and labels |
| ADR-015 | Multi-tenant SaaS isolation | independent clinic customer as tenant; branches below tenant; separate platform control plane |
| ADR-016 | Platform support access | no standing clinical access; time-bound reasoned elevation with immutable audit |
| ADR-017 | Internal and public identifiers | `uint` CLR Identity IDs, signed application-generated business `bigint` keys and tenant/type-bound protected strings publicly |
| ADR-018 | Architecture and legacy authority split | SDTS owns topology/Identity; BookDocAppointment supplies validated contract/booking/payment behavior only |
| ADR-019 | Product host boundary | API, Admin and Portal are product applications; AppHost is development-only orchestration |
| ADR-020 | Frontend exposure and reuse | IP/network-restricted ERP Admin; authenticated internet Portal/mobile; separate reusable Blazor and MAUI UI libraries |
| ADR-021 | MAUI development composition | MAUI Blazor Hybrid Portal-oriented shell; shared Client/Blazor UI plus native MAUI UI; explicit-start Windows resource in development AppHost |
| ADR-022 | Messaging, templates and document generation | API-composed class libraries; Messaging depends on Templates; Worker owns durable execution; DocumentService exports neutral models to Open XML/PDF without owning storage |
| ADR-023 | Cross-host error handling | One platform-neutral ErrorHandling library; API/Worker/Client adapters remain host-specific; UI presentation remains in Blazor/MAUI projects |
| ADR-024 | Operational evidence separation | Diagnostic telemetry, security/operation audit and domain history are separate evidence types with different owners and retention |
| ADR-025 | Correlation and observability | OpenTelemetry-compatible host defaults, privacy-safe dimensions and trace/correlation propagation through requests and durable jobs |
| ADR-026 | Configuration inheritance | Typed allow-listed platform, tenant, organization and branch settings with effective-version history |
| ADR-027 | Secrets and key lifecycle | Managed secret references, scoped credentials, rotation overlap and tested recovery; never ordinary settings or committed values |
| ADR-028 | Feature rollout | Scoped, owned, expiring and audited feature flags that never replace permission checks |
| ADR-029 | API lifecycle and workload protection | Explicit major versions, bounded queries/payloads, optimistic concurrency, rate limits and durable idempotency |
| ADR-030 | Incoming integration reliability | Verified replay-safe durable inbox plus reconciliation for ambiguous provider outcomes |
| ADR-031 | File lifecycle | Quarantine, scan, classify, checksum, authorize, retain/legal-hold and evidenced deletion through an Infrastructure storage adapter |
| ADR-032 | Query acceleration | Indexed SQL/projections first; tenant-safe cache/search only after measured need and never as system of record |
| ADR-033 | Localization and accessibility | Canonical persisted values, `en-IN` initial default, explicit currency/time zone and WCAG 2.2 AA web target |
| ADR-034 | Supply chain and release integrity | Reproducible dependencies, scanning, SBOM, signed artifacts and expand/migrate/contract database delivery |
| ADR-035 | Resilience and continuity | Capability-specific SLO/RPO/RTO, explicit degraded behavior and fully reconciled restore exercises |

## Change request template

```text
Change ID / requester / date:
Requested outcome and reason:
Affected release/modules/personas:
Data/API/UI/security/privacy/operation impact:
Migration/backward compatibility impact:
Estimate and schedule impact:
Risks and alternatives:
Decision/approver/date:
Documents/backlog/tests updated:
```

## Feature traceability row

```text
Requirement ID:
Legacy evidence:
Approved behavior change:
Module/use case:
Permission/scope:
Contract endpoint/version:
Tables/migration:
Automated test IDs:
Migration mapping/reconciliation:
UAT owner/evidence:
Release/status:
```

## Database migration review template

```text
Migration name/hash:
Owning module/schema:
Additive/breaking/destructive classification:
Tables/columns/indexes/constraints affected:
Volume and lock/log impact:
Backfill/checkpoint plan:
Application compatibility window:
Forward-fix/rollback plan:
Backup/restore prerequisite:
Security/scope/audit review:
Integration/performance evidence:
Reviewer/approval/date:
```

## Rehearsal report template

```text
Snapshot/schema hashes and extraction watermark:
Tool/release/commit and configuration versions:
Environment and database size:
Start/end/duration by wave:
Source disposition counts:
Relationship/constraint results:
Financial control totals/variances:
Quarantine count and top reason codes:
Performance/resource observations:
Smoke/security test results:
Rollback/restore result:
Issues, owners and due dates:
Go/no-go assessment and approvals:
```

## Six-hour session handoff template

```text
Session date / owner / backlog ID:
Acceptance outcome attempted:
Files/migrations/contracts changed:
Tests run and results:
Manual evidence:
Decisions/assumptions made:
Risks or blockers:
Exact next action:
Master/register updates:
```

## Navigation maintenance checklist

When adding or changing an artifact:

1. Link it from the root master or the closest owning document.
2. Add/update its register row here.
3. Update affected ADR, schema, migration, test and roadmap references.
4. Verify all relative links and headings.
5. Mark superseded material; do not silently overwrite final release/rehearsal evidence.
6. Record last review and owner for operational documents.

## Review cadence

- Per implementation session: backlog/handoff and affected technical document.
- Weekly: master phase/status, risks, decisions, estimates and register links.
- Per database change: data dictionary/migration review/reconciliation.
- Per release: immutable release evidence, known issues, go/no-go, rollback.
- Per Worker/reporting/printing gate: task inventory, parallel-run reconciliation, report dispositions/golden comparisons, schedule/delivery evidence and print-agent branch/offline evidence.
- After incident or migration rehearsal: runbook and test gaps updated within the corrective action.
