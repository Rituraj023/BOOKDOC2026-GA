# 03 — Target Architecture and New Logic

## Target solution shape

Use the SDTS-derived modular-monolith composition. The following project names and boundaries are now implemented, including the first MAUI host and reusable native UI boundary.

```text
BookDoc2026.AppHost              development-only orchestration of API/Admin/Portal and explicit-start MAUI Windows
BookDoc2026.ServiceDefaults      health, telemetry and service defaults
BookDoc2026.Worker               class library for API-hosted immediate/scheduled jobs
BookDoc2026.Templates            scoped/versioned templates and strict rendering
BookDoc2026.Messaging            channel-neutral dispatch and provider contracts
BookDoc2026.DocumentService      Open XML/PDF generation and export contracts
BookDoc2026.ErrorHandling        cross-host error vocabulary, mapping and recovery policy
BookDoc2026.Bootstrap            one-time first-platform-operator provisioning command

BookDoc2026.Api                  HTTP composition, middleware, JWT, versioned endpoints
BookDoc2026.Application          use cases, validators, ports, domain orchestration
BookDoc2026.Domain               aggregates, Identity entities, value objects and invariants
BookDoc2026.Infrastructure       EF Core, Identity stores, JWT issuance, queues, migrations
BookDoc2026.Contracts            versioned request/response contracts and permissions
BookDoc2026.Shared.Kernel        small shared security/platform vocabulary
BookDoc2026.Client               typed API clients used by all UI hosts

BookDoc2026.Blazor.UI            shared presentation components only
BookDoc2026.Admin                ERP-style, network/IP-restricted administration and reports/import/export
BookDoc2026.Portal               authenticated internet-facing operations and self-service
BookDoc2026.Mobile               MAUI host and platform adapters
BookDoc2026.Maui.UI              reusable MAUI components shared by mobile hosts

Tests: Unit | Integration | Architecture | future host/UI/Mobile suites
```

Dependency rules:

```text
Domain <- Application <- Infrastructure
             ^                ^
Contracts <- Client        API composition
      ^         ^
      +---- Admin / Portal / MAUI
```

- Clinical/business domain code references no EF, HTTP, UI or provider SDK. The isolated Identity model may reference ASP.NET Core Identity store abstractions, following the SDTS model; other aggregates must not depend on Identity framework types.
- Application owns use cases and declares ports. Infrastructure implements ports.
- API translates HTTP to application requests and contains no clinical or financial rules.
- Contracts contain wire models, never EF entities.
- ErrorHandling contains platform-neutral codes, categories, safe descriptors and recovery guidance. API owns HTTP translation; Blazor/MAUI own visual presentation; Worker owns retry/dead-letter behavior.
- ASP.NET Core Identity users/roles use `uint` CLR IDs; business aggregates use positive signed `long` keys. SQL Server stores both as `bigint` where required. Routes and DTOs use tenant/type-bound protected strings. Decode at the HTTP/application boundary and still enforce authorization. See [DOC-031](31-numeric-and-protected-identifier-reference.md).
- Client is the only normal HTTP protocol implementation used by Admin, portals, and MAUI.
- A module cannot query or update another module's tables. Use an application contract, read model, domain event, or outbox integration event.

## Module map

| Module/schema | Owns | Clinic MVP | Future consumers |
|---|---|---:|---|
| Platform (`identity`, `platform`, `audit`, `messaging`, `scheduler`, `files`) | independent-clinic tenant lifecycle, identity, permissions, scopes, audit, outbox, notifications, jobs, documents | Yes | All |
| Organization (`org`) | organization, clinic, branch, room, settings, numbering | Yes | Hospital, inventory, finance |
| Patient Registry (`patient`) | patient identity, contacts, relations, consent, alerts, merge lineage | Yes | All care modules |
| Workforce (`workforce`) | employee, practitioner, specialty, qualifications, branch assignment | Yes | Hospital, HR |
| Catalog/Resources (`catalog`, `resource`) | services, procedures, categorized practitioners/spaces/beds/modalities/equipment/teams, medications, investigations, prices | Yes | Billing, lab, radiology, pharmacy |
| Scheduling (`scheduling`) | resource availability, slot/hold, request, multi-resource booking, waitlist | Yes | Inpatient procedures, imaging, OT |
| Queue (`queue`) | service points, tickets, stages, assignment, priority, SLA events | Yes | OPD, X-ray/CT/MRI, lab, pharmacy, billing |
| Clinical (`clinical`) | encounter, observations, diagnoses, notes, prescriptions, orders | Yes | Inpatient, lab, pharmacy |
| Billing (`billing`) | invoice, receipt/payment, allocation, refund, close | Yes | Hospital/insurance |
| Reporting (`reporting`) | approved read models, exports, definitions | Yes | All |
| Printing (`printing`) | paired agents/printers, receipt/slip/label jobs and events | Staged | Front desk, lab, billing |
| Inpatient (`inpatient`) | admission, episode, ward/bed transfer, discharge | Future | Nursing, billing |
| Radiology (`radiology`) | modality worklist, study/protocol/report metadata and PACS integration | Clinic phase after queue foundation | Hospital |
| Nursing/Lab/Pharmacy/OT/Emergency/Insurance | their own workflows and tables | Future | Hospital |
| Workforce/Payroll integration | external employee/payroll ownership map and adapters | Deferred until source supplied | Organization/clinical scheduling |

## Standard vertical slice

Every non-trivial use case contains:

```text
Modules/<Module>/
  Domain/Entities, ValueObjects, Events
  Application/<UseCase>/Request, Handler, Validator, Result
  Application/Ports
  Contracts/V1
  Infrastructure/Persistence, Integrations
  Api/EndpointsOrControllers
  Tests/Domain, Application, Integration, Authorization
```

Every slice definition must include aggregate/invariant, scope, permission, request idempotency, concurrency, audit, database indexes, API contract, failure codes, client method, UI states, and automated tests.

## New business logic

### Patient identity and merge

1. Assign a human-readable patient number from a branch-safe numbering service; use a separate internal key.
2. Normalize mobile/email for search while preserving display form.
3. Generate duplicate candidates using configurable combinations (for example mobile+DOB or name+DOB), but require authorized human confirmation.
4. Merge by moving/repointing allowed dependents in one transaction, retaining both identifiers and a `patient_merge` lineage record.
5. Never delete the losing patient physically; mark it merged and redirect authorized lookups.
6. Block merge when open financial/clinical conflicts require manual resolution.

### Scheduling state machine

```text
SlotAvailable -> Held -> Booked -> CheckedIn -> InProgress -> Completed
                         \-> Cancelled
                         \-> NoShow
Waitlisted -> Offered -> Held/Booked or Expired
Booked -> Rescheduled (old record terminal, new record linked)

AppointmentRequest:
Draft -> Submitted -> UnderReview -> Offered -> Accepted (creates booking)
                   \-> Declined
                   \-> Cancelled/Expired
```

Rules:

- Slot search is advisory; booking is protected by a database constraint/concurrency transaction.
- A guest or “quick appointment” request is not a booking. It has its own state, verification/anti-abuse controls and staff decision trail; only acceptance through slot allocation creates a confirmed appointment.
- Holds expire through a scheduler job and are idempotent.
- A request idempotency key prevents double submission from web/mobile retries.
- Appointment stores planned UTC start/end plus source time zone and local operational date.
- Reschedule creates lineage; it does not erase original timing/history.
- State transitions are explicit commands and rejected from invalid states.
- Overbooking is a named permission plus reason, never an accidental race result.

### Clinical record lifecycle

```text
Draft -> Signed -> Amended
```

- Only a draft is edited in place.
- Signing records author, credential/role context, UTC time, content version/hash, and encounter state.
- A signed record is immutable. Corrections append an amendment that names the prior version and reason.
- Measurements store numeric value, unit, capture time, source, and optional reference/abnormal flag.
- Diagnosis and medication can include standard codes, but preserve original clinician text.
- Sensitive notes may require a narrower permission than general encounter access.
- Printing/exporting/share-link creation is authorized and audited.

### Prescription lifecycle

- Draft medications have structured drug, strength, form, dose, unit, route, frequency, duration, quantity, instructions, and substitution fields.
- Issue validates required fields and creates an immutable prescription version.
- Discontinue/cancel creates a separate status event with reason; it does not delete an issued item.
- Allergy/interaction warnings require defined data sources and responsibility. Do not imply automated safety checks until an approved provider exists.

### Billing lifecycle

```text
Draft Invoice -> Issued -> PartPaid -> Paid
                         \-> Voided (authorization + reason, no deletion)
Payment -> Allocated -> Part/Full Refund or Reversal
Open Cash Session -> Counted -> Closed
```

- Invoice line snapshots description, quantity, unit price, discount, tax code/rate/amount, and total.
- Posted money records are immutable; use adjustments, credit/refund, and reversal records.
- Currency arithmetic uses fixed precision; rounding policy is centralized and tested.
- Invoice/receipt numbers come from transactional scoped sequences and are unique.
- Payment provider callbacks are idempotent and verified.

## Cross-cutting platform behavior

The detailed capability ownership, telemetry/audit separation, configuration hierarchy, API lifecycle, durable inbox, storage lifecycle, localization and continuity rules are governed by [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md). Apply those rules through the existing projects unless a proposed library passes the explicit project-creation gate.

### Scope and permissions

Use ASP.NET Core Identity roles/claims plus durable `ApplicationUserScope` assignments with `Tenant -> Organization -> Branch`. A tenant represents one independent clinic customer and is the primary isolation boundary; clinic records normally require tenant, organization, and branch. The database proves that a scoped Branch belongs to the same Tenant and Organization. JWT access tokens carry the selected durable scope, while hashed refresh tokens are rotated and remain bound to that scope. Financial year is optional and should only appear in modules that need it. Platform-operator access uses a separate control-plane authorization path with no standing clinical access, as defined in [Product Vision, Scope and Tenancy](12-product-vision-scope-and-tenancy.md).

Development/testing may use the explicit header handler. Every other environment authenticates with bearer JWT. Identity users, roles and scope assignments are durable database records; request headers never create authority.

Example resource/action permissions:

- `Patients.View`, `Patients.Create`, `Patients.Edit`, `Patients.Merge`, `Patients.Export`
- `Appointments.View`, `Appointments.Book`, `Appointments.Reschedule`, `Appointments.Cancel`, `Appointments.Overbook`
- `Encounters.View`, `Encounters.EditDraft`, `Encounters.Sign`, `Encounters.Amend`, `Encounters.Export`
- `Billing.Invoice`, `Billing.DiscountApprove`, `Billing.Receive`, `Billing.Refund`, `Billing.CloseDay`
- `Documents.View`, `Documents.Upload`, `Documents.Delete`
- `Audit.View`, `Users.Manage`, `Roles.Manage`, `Reports.Export`

Authorization tests must combine role permission and durable scope. A valid token with a forged branch header must fail.

### Concurrency and idempotency

- Add row-version concurrency to appointment, availability exception, encounter draft, invoice draft, patient demographics, and other sensitive mutable aggregates.
- Accept idempotency keys on booking, payment, notification enqueue, document upload initiation, and external callbacks.
- Return stable conflict codes (`slot_taken`, `stale_version`, `duplicate_request`) that clients can handle.

### Events and side effects

- Domain events handle in-process consistency without network calls.
- Persist outbox messages in the same transaction as state changes.
- Workers deliver notification/integration events with retry and idempotent consumers.
- API endpoints authorize, enqueue and monitor durable work. The API process hosts immediate and scheduled services registered from the `BookDoc2026.Worker` class library.
- Worker-library services share Application/Infrastructure use cases and never call API endpoints over HTTP to perform internal jobs.
- `BookDoc2026.Messaging` depends on `BookDoc2026.Templates`; provider adapters implement its contracts without placing credentials or HTTP calls in templates.
- `BookDoc2026.DocumentService` generates deterministic byte artifacts from a neutral document model. Storage, authorization and immutable snapshot ownership remain outside the exporter.
- Arbitrary SQL or stored-procedure strings are not job definitions; every job maps to a named typed handler.
- Never call SMS, WhatsApp, email, payment, or file providers inside the core transaction.

### Documents and audit

- Adapt the SDTS document abstraction: opaque storage key, hash, size, category, owner, scope, lifecycle status.
- Use `BookDoc2026.DocumentService` for Open XML DOCX and PDF bytes, safe filenames and SHA-256 output metadata. Do not mix uploaded-document storage with document generation.
- Add malware scanning/quarantine before public uploads are considered available.
- Audit mutation plus high-risk reads/exports. Redact clinical text, tokens, destinations, message bodies, secrets, and document content from structured logs.

## API conventions

- `/api/v1/...`, consistent envelope and validation errors, correlation/trace ID.
- Resource queries use typed filters with field allow-lists, maximum page size, maximum filter depth, and server-side scope.
- Commands use explicit endpoints (`appointments/{id}/check-in`) rather than generic PATCH for state transitions.
- Publish OpenAPI and run compatibility checks. Breaking changes require a new API version.
- Use problem-specific result codes; UI must not parse human message text.
- Error responses include stable code, category, safe message, trace identifier and transient flag. Unknown exceptions never expose raw exception/database/provider text.

## UI composition

- Admin: ERP-style platform and tenant administration for organizations/branches, users/roles/scopes, catalogs, integrations, jobs, audit, report management, exports and imports. Access requires authentication/permission and an approved network/IP path enforced at the trusted edge and host.
- Platform control plane: clinic application review, verification, approval/rejection, tenant provisioning/lifecycle, service health and reasoned support elevation; it is not a cross-tenant clinical dashboard.
- Portal: authenticated internet-facing dashboards and workspaces for permitted staff and patient self-service. “Global access” means reachable outside the Admin network; it never means cross-tenant visibility.
- MAUI: focused mobile versions of approved Portal workflows; do not reproduce Admin management screens.

Blazor components reusable across Admin and Portal belong in `BookDoc2026.Blazor.UI`. MAUI components reusable across mobile applications belong in `BookDoc2026.Maui.UI`. Business rules, API calls and authorization do not live in either UI component library.

Admin owns report management, approved schedules, execution history and print-agent administration. Portal exposes permission-approved operational reports and direct clinical/financial documents. Both use one report catalog and the same server-side scope/permission enforcement. See [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).

Admin and Portal both support permission-composed dashboards for any role. Host access, dashboard access, card data and card commands are authorized independently. Role names may suggest defaults, but permissions and durable scope determine actual access. See [Role Dashboards, Resource Booking, Queues and Realtime Messaging](10-role-dashboards-resource-queue-realtime-messaging.md).

## Realtime presentation rule

SignalR publishes minimal invalidation/state envelopes to authorized user/scope/service-point groups. Clients reload the authoritative projection through the typed API, compare entity versions and recover after missed/out-of-order events. Durable changes and notifications originate from committed domain/outbox events; SignalR is not a transaction, audit store or guaranteed-delivery queue.

SignalR may announce that a background job, report execution or print job changed status, but it never performs the durable job or replaces API status retrieval.

## Definition of architecture complete

- Architecture tests prevent forbidden project/module dependencies.
- A reference vertical slice demonstrates command/query, validation, authorization/scope, EF mapping/migration, typed client, UI, audit, concurrency, and tests.
- CI builds all applications and libraries, runs unit/integration/architecture/UI smoke tests, and checks formatting/packages/migrations/OpenAPI.
- Secrets are externalized; logs and audit exclude sensitive payloads; health checks and telemetry work through ServiceDefaults.

The implemented architecture/boundary status and the contract/booking/payment preservation sequence are controlled by [SDTS Architecture Realignment and Legacy Business Preservation](32-sdts-architecture-realignment-and-legacy-business-preservation.md).

The architecture is not complete merely because these boundaries are documented. The first affected vertical slices must provide the operational evidence required by [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md).
