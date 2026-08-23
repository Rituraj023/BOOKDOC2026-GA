# 33 — Architecture Understanding and Achievement Report

Owner: Product + architecture + delivery  
Status: Evidence-based checkpoint  
Reviewed: 2026-08-23

## Purpose and scoring

This report records twenty important conclusions understood from the planning discussions, legacy evidence and implemented BOOKDOC2026 foundation. It separates an architectural decision from working product behavior so a scaffold is never reported as a finished clinic-management feature.

Each percentage is a directional readiness indicator:

- `100%` means the currently agreed boundary is implemented and verified for its present scope;
- `50%` means a meaningful reference foundation exists but the end-to-end user outcome is incomplete;
- `0%` means the subject is only an idea with no approved plan or implementation evidence.

The exact simple average across these twenty concerns is now **73.25%** (73% rounded). This means the architecture and several reference foundations are materially established; it does **not** mean the clinic MVP is 73.25% production-ready. Specialty clinical delivery, broader financial policy, real-provider, reporting, representative migration and operational acceptance remain major delivery work.

## Twenty-point understanding and achievement assessment

| # | What is understood | Current evidence and achievement | Score | Main work remaining |
|---:|---|---|---:|---|
| 1 | SDTS is the authority for topology, Identity, JWT, roles, permissions and UI/client boundaries; the old BOOKDOC systems are evidence for validated business behavior, not a code architecture to copy. | The authority split is explicit in [DOC-032](32-sdts-architecture-realignment-and-legacy-business-preservation.md). | 90% | Obtain live-user and production-data confirmation for remaining legacy rules. |
| 2 | API, Admin and Portal are the three main product applications. Other projects are libraries, tests, commands or development tooling. | The solution and architecture tests use this boundary; AppHost is not treated as a product deployment. | 100% | Keep future modules from creating accidental new backend hosts. |
| 3 | API is the only backend host and owns HTTP commands, database composition, authorization, messaging administration and immediate/scheduled service hosting. | `BookDoc2026.Api` composes Infrastructure, Worker, Messaging, Templates, DocumentService and ErrorHandling. DOC-047 adds Clinical and [DOC-048](48-practitioner-credential-and-assignment-foundation.md) adds Workforce practitioner eligibility through the same protected, permissioned host boundary. | 90% | Add remaining domain modules, provider adapters, production monitoring and operational controls. |
| 4 | Worker must remain a standalone project but as a reusable class library; it works inside API and must not call API. | `BookDoc2026.Worker` is a class library registered by API. [DOC-036](36-durable-messaging-reference-slice.md) proves operation/correlation-aware typed handling, deterministic transient retry, permanent dead-letter, accepted-operation replay protection and safe error-code persistence. | 85% | Implement scheduled task families, dead-letter administration, tenant fairness/multi-instance load and Windows-service parallel-run/retirement evidence. |
| 5 | Aspire AppHost is required for development orchestration only. It starts API/Admin/Portal and offers MAUI Windows as an explicit-start resource. | `BookDoc2026.AppHost/Program.cs` injects the live API endpoint into all UI hosts; `mobile-windows` waits for API and does not auto-start. | 95% | Add development dependencies only when selected (for example SQL/observability) and document team launch profiles. |
| 6 | Admin is an ERP-style management frontend, intended for approved IP/network access, and owns platform control, reporting management, schedules, import and export. | DOC-037/038 provide communication management; DOC-039 adds server-circuit authentication/network controls; DOC-043/044 add radiology setup/dashboard cards; [DOC-050](50-portal-cashier-and-admin-billing-oversight.md) adds independently permission-filtered, read-only Invoice/Payment oversight. | 70% | Approve durable privileged-session/MFA policy; deploy and externally test real network/proxy configuration; implement broader control-plane dashboards/screens, reports, import/export and audit/job operations. |
| 7 | Portal is authenticated and internet-facing, but every result remains permission-, tenant-, organization- and branch-scoped. | DOC-043–045 prove technician/reception journeys; [DOC-050](50-portal-cashier-and-admin-billing-oversight.md) adds a complete permission-composed Cashier transaction with protected IDs, retained retry identities and browser receipt printing. | 75% | Decide final browser-session/device policy, add remaining operational/patient journeys and prove broad accessibility/clinic/finance acceptance. |
| 8 | Mobile is a focused mobile version of approved Portal workflows, not a copy of Admin and not a separate business backend. | A .NET 10 MAUI Blazor Hybrid shell targets Android, iOS, Mac Catalyst and Windows and consumes the same typed error vocabulary and recovery policy through Client. Windows and Android builds pass. | 50% | Implement approved M1 journeys, device/session lifecycle, native error presentation, navigation, accessibility, deep links, push and device tests. |
| 9 | Shared Razor components belong in `BookDoc2026.Blazor.UI`; reusable native controls and platform services belong in `BookDoc2026.Maui.UI`. | DOC-043/044 add Queue and permission components; DOC-045 renders them in a real browser, corrects the shared event-directive import and proves interactive queue actions. Mobile renders shared `ScopeBadge`, while MAUI UI owns native registration/secure storage. | 70% | Establish full design tokens/layouts, further workflow components, broad WCAG evidence, CI component/browser tests and MAUI reuse of approved Portal journeys. |
| 10 | Admin, Portal and Mobile use one versioned typed Client/Contracts boundary. Mobile refresh credentials require device secure storage while short-lived access tokens remain memory-first. | `BookDoc2026.Mobile` uses Client plus secure device refresh storage; DOC-039 proves Admin login, near-expiry rotation and revoke through the same Auth Client while retaining credentials only in server circuit memory. | 75% | Prove replay response, revocation purge, biometric policy and API error handling on devices; add compatibility and shipped-host end-to-end evidence. |
| 11 | Authentication follows the new SDTS-derived Identity model: `uint` user/role IDs, claims permissions, durable scopes, JWT access and hashed rotating refresh tokens. | Identity entities, migrations, login/refresh/revoke flows and bootstrap exist; DOC-039 proves Admin session rotation, expiry clearing and revoke consumption. | 85% | Complete MFA/privileged policy, production signing/Data Protection operations, durable session/device administration and access reviews. |
| 12 | The SaaS serves multiple independent clinic tenants; tenant → organization → branch scope is durable, and platform approval is required before activation. | Onboarding/approval/provisioning, branch configuration, durable user scopes and isolation tests exist. | 75% | Complete platform control-plane UX, tenant lifecycle, support elevation and scale/load evidence. |
| 13 | A Stakeholder/Party can be a person or corporate body and centrally owns contacts, addresses, identifiers and documents; Patient and Practitioner are separate roles linked to a person. | Stakeholder/Patient reference slices and [DOC-048](48-practitioner-credential-and-assignment-foundation.md) prove two role aggregates reuse central Person truth without duplicating demographics or contacts. | 75% | Add duplicate resolution, document lifecycle, consent, corporate relations, remaining party roles and production migration maps. |
| 14 | Booking is generalized beyond doctors: practitioners, rooms, beds/chairs, imaging modalities, equipment and teams can satisfy categorized requirements. | DOC-040 adds confirmed generalized Booking. [DOC-041](41-booking-lifecycle-and-waitlist-reference-slice.md) adds versioned cancellation/reschedule, atomic capacity release/reacquisition, waitlist promotion/replay, lineage, authorization and durable patient updates. | 70% | Implement UI journeys, automatic waitlist offers/expiry, completed/no-show/overbook policy, bed admission/occupancy, efficient slots, deployed multi-connection stress and accepted clinic workflows. |
| 15 | Legacy contract/package entitlement, tariff, visit consumption, payment allocation and adjustment behavior must be preserved as rules, while generalized resources replace doctor-only assumptions. | DOC-046 implements the Contract ledger; DOC-049 adds immutable Invoice/Payment/allocation truth; [DOC-050](50-portal-cashier-and-admin-billing-oversight.md) makes the safe subset usable through Portal Cashier and read-only Admin oversight. | 50% | Obtain owner approval for automatic Booking/entitlement rules, tax/discount/refund/reversal/advance and price policy; add close/reconciliation, finance UAT and representative migration. |
| 16 | Every permitted role may receive an Admin and/or Portal dashboard; queues cover OPD, X-ray, CT and other services; SignalR carries live status, not durable commands. | DOC-042–044 implement the durable Queue and role-composed reception/technician journey; DOC-045 proves check-in and Called → Preparation → In Service → Completed across two isolated technician sessions, with SignalR invalidation and token-only display. | 85% | Automate reconnect/scale acceptance, then add OPD/remaining departments, clinical imaging stages, multi-node operations and accepted workflows. |
| 17 | Durable communication uses Worker/outbox with versioned email and WhatsApp templates plus SMS/push; SignalR is only for live UI state. | DOC-036/037/038 prove durable delivery, templates, preferences and callback reconciliation. [DOC-040](40-generalized-booking-and-preference-notification-reference-slice.md) enforces current preference/quiet-hours evidence in the first patient-addressed Booking handler and records accepted or suppressed terminal attempts without destination leakage. | 70% | Add approved providers, scheduled quiet-hours deferral, verified-contact/legal-basis rules, channel fallback, raw-payload quarantine decision, dead-letter administration and production operations. |
| 18 | Admin owns report management/scheduling; Portal exposes approved operational reports. Browser/PDF is normal printing, with a constrained future branch print agent for receipts, slips and labels. | `BookDoc2026.DocumentService` generates DOCX/PDF; DOC-049 now persists immutable canonical Invoice/receipt snapshots with hashes. Legacy report counts and target controls remain governed by DOC-011. | 30% | Add rendered storage, XLSX/CSV/report read models, active-report golden comparisons, patient delivery and print-agent security/offline validation. |
| 19 | The target uses SQL Server, schema-per-module ownership, signed numeric internal keys, protected public string IDs and repeatable staged migration rather than legacy schema parity. | Reviewed EF migrations/reference schemas exist; DOC-044 applies the full chain locally, discovers and corrects a SQL Server filtered-index incompatibility, then proves all migrations applied with no model drift. | 50% | Profile the live database, build mappings, run two representative ETL rehearsals, reconcile counts/money/relations and prove rollback/restore. |
| 20 | Quality requires architecture, unit, integration, isolation, concurrency, migration, mobile/device and operational evidence; documentation is the execution control pack. | The 122-test suite is green (78 unit, 9 architecture, 35 integration). DOC-050 adds cashier-state identity/immutability and Billing-register authorization evidence while retaining DOC-049 finance invariants. | 75% | Put browser acceptance in CI; add device, security, load, deployed proxy/network, privacy-safe observability, recovery, finance/clinician UAT and release evidence. |

## MAUI/AppHost implementation checkpoint

Implemented now:

1. `BookDoc2026.Mobile` — MAUI Blazor Hybrid app for Android, iOS, Mac Catalyst and Windows.
2. `BookDoc2026.Maui.UI` — reusable native MAUI library.
3. Mobile references the existing typed Client and shared Blazor UI library, without backend-layer references.
4. Mobile API configuration accepts AppHost's dynamic `Api__BaseUrl` and otherwise uses a local-development fallback.
5. Mobile access tokens stay in process memory and refresh tokens use platform secure storage through the MAUI library.
6. AppHost registers `mobile-windows` as explicit-start, waits for API and injects the API HTTPS endpoint.
7. The solution navigator includes both MAUI projects.

Verified on 2026-08-17:

```text
AppHost Release build:              passed, 0 warnings / 0 errors
MAUI Windows Release build:         passed, 0 warnings / 0 errors
MAUI Android Release build:         passed, 0 warnings / 0 errors
Automated tests after preference/callback work: 72 passed
```

The build check does not claim iOS runtime validation, Android emulator validation, store readiness or a finished user journey. Those require device execution and the staged acceptance work in [DOC-007](07-maui-mobile-plan.md).

## Messaging, Templates and DocumentService checkpoint

Implemented now:

1. `BookDoc2026.Templates` is a .NET 10 class library with tenant/organization/branch-aware template selection, channel/culture/version identity, strict `{{Placeholder}}` rendering, missing-value rejection and HTML encoding by default.
2. `BookDoc2026.Messaging` is a .NET 10 class library that references Templates and owns email/SMS/WhatsApp/push-neutral requests, envelopes, provider contracts, idempotency propagation and template-to-provider dispatch.
3. `BookDoc2026.DocumentService` is a .NET 10 class library with format-neutral document content, exporter selection, safe filenames, SHA-256 results, Open XML DOCX generation and PDF generation.
4. PDF output uses an embedded Open Sans font resolver, avoiding a hidden dependency on Arial or the API host's operating-system fonts.
5. API is the composition/runtime host for all three libraries. They have no executable entry point and do not reference API, Infrastructure or the clinical Domain.
6. No legacy templates, embedded credentials, direct SQL bookmark calls, Windows Service loops or patient data were copied.
7. Infrastructure now supplies a database-backed published-template catalog and development email adapter; the first tenant-approval template is persisted, versioned and resolved by tenant/organization/branch specificity.
8. A typed Worker handler records append-only delivery attempts, preserves operation/correlation identity, retries only transient results and exposes safe status through an authorized API/Client query. Full evidence is in [DOC-036](36-durable-messaging-reference-slice.md).
9. Admin now manages branch template versions through a typed Client: effective catalog, create draft, strict preview, separately authorized publish, revision-safe retire and delivery monitoring. Full evidence is in [DOC-037](37-admin-communication-management-reference-slice.md).
10. Stakeholder-owned preference events and an HMAC-verified development callback inbox now provide append-only consent evidence, replay-safe durable ingress, Worker reconciliation and Admin visibility. Full evidence is in [DOC-038](38-communication-preferences-and-callback-inbox-reference-slice.md).
11. Admin now uses server-side circuit credentials, the shared API login/refresh/revoke lifecycle, permission-aware navigation and fail-closed approved-network/trusted-proxy evaluation. Full evidence is in [DOC-039](39-admin-security-foundation-reference-slice.md).
12. A generalized Booking now atomically confirms complete role-aware resource allocations and queues a patient notification whose Worker delivery is accepted or suppressed by current preference evidence. Full evidence is in [DOC-040](40-generalized-booking-and-preference-notification-reference-slice.md).
13. Booking cancellation/reschedule and waitlist promotion now preserve lineage, capacity, versions, audit and preference-controlled durable patient updates. Full evidence is in [DOC-041](41-booking-lifecycle-and-waitlist-reference-slice.md).
14. X-ray/CT Queue now has durable versioned tickets/events, permission-scoped operations, safe displays, API reconciliation and status-only SignalR invalidation. Full evidence is in [DOC-042](42-imaging-queue-and-realtime-reference-slice.md).
15. Admin now configures imaging service points and Portal provides an authenticated technician worklist/safe display through real shared Blazor components. Realtime reconciliation explicitly respects probabilistically protected public IDs. Full evidence is in [DOC-043](43-radiology-admin-portal-journey.md).
16. Shared permission cards now compose Admin/Portal dashboards for any role; Portal reception searches patients and issues idempotent safe imaging tokens. The full migration chain also passes against the local development SQL Server after correcting a filtered-index predicate. Full evidence is in [DOC-044](44-permission-dashboards-and-radiology-reception.md).
17. An explicit development-only fixture command plus isolated browser sessions now prove reception check-in, interactive technician transitions, token-only display and cross-session SignalR reconciliation. Full evidence is in [DOC-045](45-development-fixtures-and-browser-acceptance.md).
18. A policy-neutral Contract/Package ledger now proves generalized service/resource-category eligibility, confirmed-Booking reservation, consumption/release, authorization, concurrency, idempotency and SQL persistence without prematurely coupling unresolved cancellation/refund policy. Full evidence is in [DOC-046](46-contract-package-entitlement-reference-slice.md).
19. A common Clinical Encounter now proves append-only drafts, new immutable signed revisions, reasoned amendments, protected API identity, safe audit metadata and cross-tenant rejection without inventing unapproved specialty protocols. Full evidence is in [DOC-047](47-encounter-revision-signing-foundation.md).
20. A Workforce Practitioner now reuses Person Stakeholder and Identity truth, owns verified credentials and effective branch/service assignments, optionally links practitioner resources, and gates clinical signing without introducing Employee or Payroll fields. Full evidence is in [DOC-048](48-practitioner-credential-and-assignment-foundation.md).
Additional post-report evidence includes immutable Billing truth in DOC-049 and the permission-composed Portal Cashier/Admin oversight journey in [DOC-050](50-portal-cashier-and-admin-billing-oversight.md). These do not change the report's fixed twenty scoring rows; their evidence is attached to rows 6, 7, 15, 18 and 20.

Still intentionally incomplete:

- Platform-level tenant/organization template defaults, production persistent privileged-session/MFA policy, deployed network/proxy validation, broad role dashboards and full accessibility/clinic acceptance.
- Scheduled quiet-hours deferral, verified-contact/legal-basis policy, concrete production email/SMS/WhatsApp/push providers and provider-specific callback verification.
- Dead-letter/replay administration, scheduled message families, tenant fairness and legacy Windows-service parallel-run evidence.
- Document storage, immutable snapshot persistence, authorization, signing, spreadsheet export and clinical/report layouts.
- Complex HTML-to-PDF rendering. The current PDF exporter consumes the safe neutral document model; approved HTML report rendering remains a separate reporting concern.

## Global error-handling checkpoint

The adopted design is one shared core, not one universal runtime handler:

1. `BookDoc2026.ErrorHandling` is a platform-neutral .NET 10 library containing stable error codes, categories, safe descriptors, trace-aware occurrences, transient classification, recovery recommendations, an extensible exception mapper/resolver and `RemoteErrorException`.
2. API keeps the HTTP adapter because status codes and response writing are web concerns. It uses `IExceptionHandler`, maps known domain exceptions, hides unknown exception messages and returns code/category/message/trace/transient fields.
3. Client performs error-body parsing once for every typed client and preserves the structured occurrence. It safely handles non-BOOKDOC proxy/gateway bodies without exposing their content.
4. Blazor and MAUI may independently render dialogs, pages, validation summaries or retry controls using the same category/recovery vocabulary; UI components do not belong in the core library.
5. Worker uses the same resolver for structured code/category/transient logging while preserving its own retry/dead-letter rules.
6. Domain exceptions remain in Domain. Moving them into a global library would reverse the dependency boundary and make infrastructure vocabulary leak into business rules.

This foundation does not yet implement global UI error boundaries, toast/dialog design, offline-mobile errors, provider-specific mappings, validation field dictionaries or telemetry alert routing.

## Cross-cutting architecture hardening checkpoint

[DOC-034](34-cross-cutting-platform-hardening.md) adds the platform decisions that were not owned by a single business module:

1. Diagnostic telemetry, security/operation audit and domain history are separate evidence types rather than one overloaded log.
2. `BookDoc2026.ServiceDefaults` is the intended shared composition point for OpenTelemetry-compatible host defaults; no new observability project or vendor is assumed.
3. Configuration is typed and inherits only through allow-listed platform, tenant, organization and branch scopes. Secrets and feature flags have separate lifecycles.
4. API evolution now has explicit rules for versioning, bounded pagination/payloads, concurrency, rate limits and durable idempotency.
5. Provider callbacks gain a future durable inbox and reconciliation boundary complementary to the existing outbound outbox.
6. Storage gains a quarantine/scan/classification/checksum/retention lifecycle without turning DocumentService into a repository.
7. Cache and search remain derived optimizations introduced only after measurements; tenant and authorization scope are part of every key/query.
8. Localization, INR/time-zone handling, WCAG 2.2 AA, supply-chain integrity and capability-specific disaster recovery are explicit readiness concerns.
9. A proposed shared project must prove multiple consumers, stable behavior, one-way dependencies, clear ownership and tests. This prevents a catch-all `Platform` or `Utilities` library.

DOC-034 itself remains architecture guidance rather than implementation evidence. Subsequent verified slices through DOC-050 raise the exact score to **73.25%**: role-facing Billing advances Admin, Portal and finance usability, while production/policy/acceptance criteria remain open.

## Path to 100%

[DOC-035](35-path-to-100-percent-achievement.md) is the execution and score-control plan for moving every row to 100%. It defines per-row completion evidence, ordered workstreams, phase gates, the first twelve six-hour sessions and the final independent acceptance audit. The score changes only when implementation and acceptance evidence are linked; the current evidence-based score is **73.25%**.

## Recommended next implementation slice

Implement the Workforce/Practitioner credential and branch/service assignment foundation independently of Employee/Payroll. Then bind Encounter signing permissions to an active, appropriately assigned practitioner when the clinical owner approves the author/supervisor/countersigner matrix. Continue DOC-024 clinician workshops before implementing Physiotherapy/Orthopaedics-specific fields, outcome tools, red flags, procedures or release documents.
