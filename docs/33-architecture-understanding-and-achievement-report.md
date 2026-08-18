# 33 — Architecture Understanding and Achievement Report

Owner: Product + architecture + delivery  
Status: Evidence-based checkpoint  
Reviewed: 2026-08-18

## Purpose and scoring

This report records twenty important conclusions understood from the planning discussions, legacy evidence and implemented BOOKDOC2026 foundation. It separates an architectural decision from working product behavior so a scaffold is never reported as a finished clinic-management feature.

Each percentage is a directional readiness indicator:

- `100%` means the currently agreed boundary is implemented and verified for its present scope;
- `50%` means a meaningful reference foundation exists but the end-to-end user outcome is incomplete;
- `0%` means the subject is only an idea with no approved plan or implementation evidence.

The exact simple average across these twenty concerns is now **62%**. This means the architecture and several reference foundations are materially established; it does **not** mean the clinic MVP is 62% production-ready. Clinical, financial, real-provider, reporting, migration and operational journeys remain the major delivery work.

## Twenty-point understanding and achievement assessment

| # | What is understood | Current evidence and achievement | Score | Main work remaining |
|---:|---|---|---:|---|
| 1 | SDTS is the authority for topology, Identity, JWT, roles, permissions and UI/client boundaries; the old BOOKDOC systems are evidence for validated business behavior, not a code architecture to copy. | The authority split is explicit in [DOC-032](32-sdts-architecture-realignment-and-legacy-business-preservation.md). | 90% | Obtain live-user and production-data confirmation for remaining legacy rules. |
| 2 | API, Admin and Portal are the three main product applications. Other projects are libraries, tests, commands or development tooling. | The solution and architecture tests use this boundary; AppHost is not treated as a product deployment. | 100% | Keep future modules from creating accidental new backend hosts. |
| 3 | API is the only backend host and owns HTTP commands, database composition, authorization, messaging administration and immediate/scheduled service hosting. | `BookDoc2026.Api` now composes Infrastructure, Worker, Messaging, Templates, DocumentService and ErrorHandling libraries. It uses the modern ASP.NET Core exception-handler pipeline and emits safe structured error occurrences. | 90% | Add remaining domain modules, provider adapters, production monitoring and operational controls. |
| 4 | Worker must remain a standalone project but as a reusable class library; it works inside API and must not call API. | `BookDoc2026.Worker` is a class library registered by API. [DOC-036](36-durable-messaging-reference-slice.md) proves operation/correlation-aware typed handling, deterministic transient retry, permanent dead-letter, accepted-operation replay protection and safe error-code persistence. | 85% | Implement scheduled task families, dead-letter administration, tenant fairness/multi-instance load and Windows-service parallel-run/retirement evidence. |
| 5 | Aspire AppHost is required for development orchestration only. It starts API/Admin/Portal and offers MAUI Windows as an explicit-start resource. | `BookDoc2026.AppHost/Program.cs` injects the live API endpoint into all UI hosts; `mobile-windows` waits for API and does not auto-start. | 95% | Add development dependencies only when selected (for example SQL/observability) and document team launch profiles. |
| 6 | Admin is an ERP-style management frontend, intended for approved IP/network access, and owns platform control, reporting management, schedules, import and export. | DOC-037/038 provide communication management. [DOC-039](39-admin-security-foundation-reference-slice.md) adds server-side circuit sign-in/refresh/revoke/expiry, permission-filtered navigation/sections and fail-closed CIDR/trusted-proxy enforcement. | 55% | Approve durable privileged-session/MFA policy; deploy and externally test real network/proxy configuration; implement broader control-plane screens, reports, import/export and audit/job operations. |
| 7 | Portal is authenticated and internet-facing, but every result remains permission-, tenant-, organization- and branch-scoped. | Portal and the shared typed Client now receive structured code/category/trace/transience through `RemoteErrorException`, allowing host-specific recovery without parsing message text. | 40% | Implement sign-in/session UX, permission dashboards, operational journeys, visible error presentation and security validation. |
| 8 | Mobile is a focused mobile version of approved Portal workflows, not a copy of Admin and not a separate business backend. | A .NET 10 MAUI Blazor Hybrid shell targets Android, iOS, Mac Catalyst and Windows and consumes the same typed error vocabulary and recovery policy through Client. Windows and Android builds pass. | 50% | Implement approved M1 journeys, device/session lifecycle, native error presentation, navigation, accessibility, deep links, push and device tests. |
| 9 | Shared Razor components belong in `BookDoc2026.Blazor.UI`; reusable native controls and platform services belong in `BookDoc2026.Maui.UI`. | Mobile renders the shared `ScopeBadge`; the new MAUI library owns native registration and secure storage. | 40% | Establish tokens, layouts, states, accessibility controls and component tests driven by real screens. |
| 10 | Admin, Portal and Mobile use one versioned typed Client/Contracts boundary. Mobile refresh credentials require device secure storage while short-lived access tokens remain memory-first. | `BookDoc2026.Mobile` uses Client plus secure device refresh storage; DOC-039 proves Admin login, near-expiry rotation and revoke through the same Auth Client while retaining credentials only in server circuit memory. | 75% | Prove replay response, revocation purge, biometric policy and API error handling on devices; add compatibility and shipped-host end-to-end evidence. |
| 11 | Authentication follows the new SDTS-derived Identity model: `uint` user/role IDs, claims permissions, durable scopes, JWT access and hashed rotating refresh tokens. | Identity entities, migrations, login/refresh/revoke flows and bootstrap exist; DOC-039 proves Admin session rotation, expiry clearing and revoke consumption. | 85% | Complete MFA/privileged policy, production signing/Data Protection operations, durable session/device administration and access reviews. |
| 12 | The SaaS serves multiple independent clinic tenants; tenant → organization → branch scope is durable, and platform approval is required before activation. | Onboarding/approval/provisioning, branch configuration, durable user scopes and isolation tests exist. | 75% | Complete platform control-plane UX, tenant lifecycle, support elevation and scale/load evidence. |
| 13 | A Stakeholder/Party can be a person or corporate body and centrally owns contacts, addresses, identifiers and documents; Patient is a role/profile linked to a person. | Stakeholder and Patient reference slices, schema and protected contracts are implemented. | 70% | Add duplicate resolution, document lifecycle, consent, corporate relations and production migration maps. |
| 14 | Booking is generalized beyond doctors: practitioners, rooms, beds/chairs, imaging modalities, equipment and teams can satisfy categorized requirements. | Resource Catalog, availability, idempotent holds, reservations and locking reference slices exist. | 55% | Implement confirmed multi-resource Booking, reschedule/cancel/waitlist, bed-category rules and accepted clinic workflows. |
| 15 | Legacy contract/package entitlement, tariff, visit consumption, payment allocation and adjustment behavior must be preserved as rules, while generalized resources replace doctor-only assumptions. | A preservation blueprint and implementation order exist. | 15% | Validate the rule matrix with users and implement Contract, Billing, refunds, close and reconciliation slices. |
| 16 | Every permitted role may receive an Admin and/or Portal dashboard; queues cover OPD, X-ray, CT and other services; SignalR carries live status, not durable commands. | Roles, dashboard composition, queue lifecycle and realtime rules are planned. | 15% | Implement first queue vertical slice, scoped projections, SignalR reconnect/invalidation and authorization tests. |
| 17 | Durable communication uses Worker/outbox with versioned email and WhatsApp templates plus SMS/push; SignalR is only for live UI state. | DOC-036/037 prove durable outbound delivery and template management; [DOC-038](38-communication-preferences-and-callback-inbox-reference-slice.md) adds append-only stakeholder preference evidence, HMAC-verified fake callbacks, replay conflict/deduplication, Worker reconciliation and safe Admin status. | 65% | Enforce approved consent policy in real stakeholder messages; add approved providers, raw-payload quarantine decision, channel fallback, dead-letter administration and production operations. |
| 18 | Admin owns report management/scheduling; Portal exposes approved operational reports. Browser/PDF is normal printing, with a constrained future branch print agent for receipts, slips and labels. | `BookDoc2026.DocumentService` now generates neutral document models as Open XML DOCX and PDF with SHA-256 metadata and an embedded deterministic font. Legacy report counts and target permissions/retention remain governed by [DOC-011](11-backend-workers-reporting-printing.md). | 25% | Add XLSX/CSV/report read models, storage/snapshot integration, active-report golden comparisons and print-agent security/offline validation. |
| 19 | The target uses SQL Server, schema-per-module ownership, signed numeric internal keys, protected public string IDs and repeatable staged migration rather than legacy schema parity. | Reviewed EF migrations and reference schemas exist; migration and reconciliation plans exist. | 45% | Profile the live database, build mappings, rehearse ETL, reconcile counts/money/relations and prove rollback. |
| 20 | Quality requires architecture, unit, integration, isolation, concurrency, migration, mobile/device and operational evidence; documentation is the execution control pack. | The suite now has 81 tests: 42 unit, 9 architecture and 30 integration. It additionally proves Admin token rotation/expiry/revoke, fail-closed CIDR rules, trusted-proxy spoof resistance and permission-navigation policy. [DOC-034](34-cross-cutting-platform-hardening.md) supplies the cross-cutting evidence model. | 70% | Implement CI/device/UI/security/load tests, deployed proxy/network tests, privacy-safe observability, audit separation, supply-chain evidence, recovery rehearsal, UAT and release evidence. |

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

Still intentionally incomplete:

- Platform-level tenant/organization template defaults, production persistent privileged-session/MFA policy, deployed network/proxy validation and full browser/accessibility acceptance.
- Consent enforcement in stakeholder-addressed dispatch, concrete production email/SMS/WhatsApp/push providers and provider-specific callback verification.
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

DOC-034 itself remains architecture guidance rather than implementation evidence. The durable messaging, Admin management, preference/callback and Admin security slices in DOC-036/037/038/039 move the exact score to **62%**.

## Path to 100%

[DOC-035](35-path-to-100-percent-achievement.md) is the execution and score-control plan for moving every row to 100%. It defines per-row completion evidence, ordered workstreams, phase gates, the first twelve six-hour sessions and the final independent acceptance audit. The score changes only when implementation and acceptance evidence are linked; the current evidence-based score is **62%**.

## Recommended next implementation slice

Implement the first confirmed generalized Booking vertical slice and its stakeholder-addressed transactional notification. Apply communication-preference evidence at dispatch, preserve outbox/Worker durability and use the development provider. Select no real provider until DOC-017 and DOC-020 approvals pass.
