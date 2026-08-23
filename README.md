# BOOKDOC2026 Implementation Master

Status: planning baseline v1 + cross-cutting hardening baseline; SDTS-aligned host/Identity, Worker/Messaging/Templates/DocumentService/ErrorHandling, MAUI/AppHost, Foundation, Stakeholder/Patient, Resource Catalog, Scheduling, generalized Booking lifecycle/waitlist, imaging Queue/SignalR, durable Messaging, Encounter, Practitioner/Workforce, Billing and role-facing Cashier/Admin oversight reference slices implemented
Prepared: 2026-08-09; architecture hardening reviewed 2026-08-18  
Current product focus: clinic management  
Future expansion: hospital management  
Main applications: API backend, Admin frontend and Portal frontend  
Supporting projects: reusable libraries plus a development-only Aspire AppHost

This is the master navigator and execution control file for rebuilding `BookDocAppointment` on the architecture proven in `SDTS.ERP2026`.

## Source systems reviewed

- Legacy product: `C:\Users\Rituraj\source\repos\Rituraj023\BookDocAppointment`
- Legacy staff Xamarin app: `C:\Users\Rituraj\source\repos\Rituraj023\BookDocApp`
- Legacy patient Xamarin app: `C:\Users\Rituraj\source\repos\Rituraj023\BookDocCustApp`
- Architecture reference: `C:\Users\Rituraj\source\repos\Rituraj023\SDTS.ERP2026`
- New implementation workspace: `C:\Users\Rituraj\source\repos\Rituraj023\BOOKDOC2026`

## Document navigator

Read and execute in this order:

| Order | File | Purpose | Main output |
|---:|---|---|---|
| 1 | [Discovery baseline](docs/01-discovery-baseline.md) | Evidence from both repositories | Confirmed inventory, strengths, risks, assumptions |
| 2 | [Legacy migration assessment](docs/02-legacy-migration-assessment.md) | Decide what can move and what must be redesigned | Feature disposition and clinic MVP scope |
| 3 | [Target architecture and new logic](docs/03-target-architecture-and-new-logic.md) | Define the product and engineering shape | Modules, dependencies, workflows, permissions |
| 4 | [Database design](docs/04-database-design-step-by-step.md) | Build the new data model safely | Schemas, tables, keys, indexes, sequencing |
| 5 | [Migration and test plan](docs/05-migration-and-test-plan.md) | Move data and prove parity | ETL stages, reconciliation, test gates, rollback |
| 6 | [Delivery roadmap and effort](docs/06-delivery-roadmap-and-effort.md) | Turn design into implementable increments | Epics, hours, dependencies, definition of done |
| 7 | [MAUI mobile plan](docs/07-maui-mobile-plan.md) | Add staff and patient mobile experiences | Mobile projects, offline rules, security, releases |
| 8 | [Document control and working templates](docs/08-document-control.md) | Keep all generated implementation files navigable | Registers, ADR/change templates, update rules |
| 9 | [Legacy Xamarin mobile discovery](docs/09-xamarin-mobile-discovery.md) | Capture staff and patient mobile evidence | Screen/workflow map, security findings, MAUI route mapping |
| 10 | [Role dashboards, resource booking, queues and realtime messaging](docs/10-role-dashboards-resource-queue-realtime-messaging.md) | Record expanded product direction | Multi-host dashboards, beds/modalities, queues, SignalR, push, email and WhatsApp |
| 11 | [Backend workers, reporting and printing](docs/11-backend-workers-reporting-printing.md) | Replace legacy service/reporting responsibilities | API-hosted Worker library, Admin/Portal reports, PDF/XLSX, schedules and print agent |
| 12 | [Product vision, scope and tenancy](docs/12-product-vision-scope-and-tenancy.md) | Establish India SaaS product boundary | Vision, tenant model, success measures and exclusions |
| 13 | [Stakeholders, roles and permissions](docs/13-stakeholders-roles-and-permissions.md) | Define actors and authorization preparation | Role baseline, host access and permission method |
| 14 | [Workflow and use-case catalog](docs/14-workflow-and-use-case-catalog.md) | Identify end-to-end business outcomes | Stable workflow IDs and exception catalog |
| 15 | [Business rules and state machines](docs/15-business-rules-and-state-machines.md) | Make transitions and invariants explicit | Proposed lifecycles and approval questions |
| 16 | [Functional requirements and acceptance](docs/16-functional-requirements-and-acceptance.md) | Make requirements traceable and testable | Requirement template, baseline requirements and evidence rules |
| 17 | [Nonfunctional, India compliance and security](docs/17-nonfunctional-india-compliance-and-security.md) | Prepare legal, privacy, security and service-quality decisions | India reference baseline and pre-pilot evidence |
| 18 | [Terminology and data ownership](docs/18-terminology-and-data-ownership.md) | Stabilize language and module ownership | Dictionary and system-of-record boundaries |
| 19 | [UI/UX navigation and screen inventory](docs/19-ui-ux-navigation-and-screen-inventory.md) | Prepare usable host experiences | Navigation candidates and required design artifacts |
| 20 | [Integrations and vendor readiness](docs/20-integrations-and-vendor-readiness.md) | Prepare external-service decisions | Integration register and vendor gate |
| 21 | [Deployment and operational readiness](docs/21-deployment-and-operational-readiness.md) | Prepare environments and production operations | SaaS controls, readiness checklist and release evidence |
| 22 | [Implementation readiness checklist](docs/22-implementation-readiness-checklist.md) | Decide when coding may safely start | Global and per-module readiness gates |
| 23 | [Pre-development decisions and questionnaire](docs/23-pre-development-decisions-and-questionnaire.md) | Track confirmed decisions and missing inputs | India/multi-tenant baseline and discovery questions |
| 24 | [Physiotherapy and Orthopaedics preparation](docs/24-physiotherapy-orthopaedics-preparation.md) | Prepare first specialty workflows | Assessments, care plans, sessions, procedures and readiness questions |
| 25 | [Pre-development architecture and workflow diagrams](docs/25-predevelopment-diagrams.md) | Communicate key product decisions visually | Tenancy, onboarding, durable Worker and readiness-gate diagrams |
| 26 | [Foundation reference slice](docs/26-foundation-reference-slice.md) | Record the first implemented backend slice | Projects, endpoints, migration, tests, local execution and remaining gates |
| 27 | [Patient Registry reference slice](docs/27-patient-registry-reference-slice.md) | Record the first patient-data backend slice | Registration, masked search, SQL Server schema, isolation and remaining gates |
| 28 | [Resource Catalog reference slice](docs/28-resource-catalog-reference-slice.md) | Establish generalized services and bookable resources | Categories, capacities, capabilities, requirements, status history and SQL evidence |
| 29 | [Stakeholder Party reference slice](docs/29-stakeholder-party-reference-slice.md) | Centralize person/corporate party data | Shared contacts, addresses, identifiers, document references and Patient linkage |
| 30 | [Scheduling Foundation reference slice](docs/30-scheduling-foundation-reference-slice.md) | Establish resource-safe scheduling primitives | Availability rules/exceptions, idempotent holds, reservations and SQL locking |
| 31 | [Numeric and Protected Identifier reference](docs/31-numeric-and-protected-identifier-reference.md) | Define internal and public identifier boundaries | Signed numeric keys, protected DTO IDs, key operations and verification gates |
| 32 | [SDTS architecture realignment and legacy business preservation](docs/32-sdts-architecture-realignment-and-legacy-business-preservation.md) | Separate architecture authority from business-behavior authority | SDTS host/Identity boundaries plus generalized contract, booking and payment blueprint |
| 33 | [Architecture understanding and achievement report](docs/33-architecture-understanding-and-achievement-report.md) | Measure current understanding and evidence without overstating completion | Twenty-point assessment, MAUI/AppHost checkpoint and next recommended slice |
| 34 | [Cross-cutting platform hardening](docs/34-cross-cutting-platform-hardening.md) | Complete the shared operational architecture without project sprawl | Observability, audit, configuration, resilience, storage, compatibility, localization and continuity rules |
| 35 | [Path to 100% architecture and clinic-MVP achievement](docs/35-path-to-100-percent-achievement.md) | Convert the twenty-point report into evidence-gated execution | Per-row completion contracts, workstreams, gates, first sessions and final audit |
| 36 | [Durable Messaging reference slice](docs/36-durable-messaging-reference-slice.md) | Prove the first DOC-035 implementation increment | Versioned DB template, outbox dispatch, safe attempts, retry and authorized status evidence |
| 37 | [Admin communication management reference slice](docs/37-admin-communication-management-reference-slice.md) | Prove the first Admin messaging workflow | Branch template versions, preview, publish/retire authorization, audit and delivery monitoring |
| 38 | [Communication preferences and callback inbox reference slice](docs/38-communication-preferences-and-callback-inbox-reference-slice.md) | Add consent evidence and inbound reconciliation | Stakeholder preference history, signed callback deduplication, Worker status events and Admin visibility |
| 39 | [Admin security foundation reference slice](docs/39-admin-security-foundation-reference-slice.md) | Secure the restricted Admin host | Server-side circuit session, refresh/revoke/expiry UX, permission-filtered navigation and fail-closed approved networks |
| 40 | [Generalized Booking and preference-controlled notification reference slice](docs/40-generalized-booking-and-preference-notification-reference-slice.md) | Confirm role-complete multi-resource holds durably | Atomic Booking/allocations/audit/outbox, protected IDs, preference enforcement and replay-safe Worker delivery |
| 41 | [Booking lifecycle and waitlist reference slice](docs/41-booking-lifecycle-and-waitlist-reference-slice.md) | Complete the first backend Booking lifecycle | Versioned cancellation/reschedule, capacity release/reacquisition, waitlist promotion and durable patient updates |
| 42 | [Imaging Queue and realtime reference slice](docs/42-imaging-queue-and-realtime-reference-slice.md) | Establish the first X-ray/CT Queue vertical foundation | Service points, idempotent tickets, versioned stages, safe displays, reconciliation API and SignalR invalidation |
| 43 | [Radiology Admin and Portal journey](docs/43-radiology-admin-portal-journey.md) | Turn the Queue foundation into the first role-facing web journey | Admin service-point catalog, Portal session/worklist/display, shared components and protected-ID-safe realtime reconciliation |
| 44 | [Permission dashboards and radiology reception](docs/44-permission-dashboards-and-radiology-reception.md) | Complete the front of the imaging Queue journey | Shared capability cards, patient search/check-in, idempotent retry and successful local migration rehearsal |
| 45 | [Development fixtures and browser acceptance](docs/45-development-fixtures-and-browser-acceptance.md) | Prove the first role-separated Portal journey in a real browser | Explicit removable fixtures, reception check-in, two-session SignalR, safe display and shared-UI corrections |
| 46 | [Contract and package entitlement reference slice](docs/46-contract-package-entitlement-reference-slice.md) | Preserve generalized package visits without doctor-only assumptions | Branch Contract ledger, service/category eligibility, Booking reservation/consumption, replay safety, migration and tests |
| 47 | [Encounter revision and signing foundation](docs/47-encounter-revision-signing-foundation.md) | Preserve clinical history without unsafe signed-note overwrite | Booking-linked drafts, immutable signed revisions, reasoned amendments, protected API, migration and tests |
| 48 | [Practitioner credential and assignment foundation](docs/48-practitioner-credential-and-assignment-foundation.md) | Prove who may perform and sign clinical services | Person/Identity linkage, verified credentials, branch/service assignments, optional practitioner resources and Encounter eligibility |
| 49 | [Billing invoice, payment and receipt foundation](docs/49-billing-invoice-payment-receipt-foundation.md) | Establish immutable posted finance without guessing refund/tax policy | Idempotent invoices/payments, split tenders, allocations, snapshots, protected API, migration and tests |
| 50 | [Portal Cashier and Admin billing oversight](docs/50-portal-cashier-and-admin-billing-oversight.md) | Make the Billing foundation usable through approved hosts | Shared financial UI, Portal transaction, read-only Admin registers, bounded APIs and tests |

Working spreadsheets, the stakeholder approval pack and its fixed PDF are indexed in the [Pre-development Artifact Package](outputs/bookdoc2026-predevelopment/README.md).

## Product boundary

Confirmed pre-development context: the primary market is India with Delhi as the first pilot jurisdiction; Physiotherapy and Orthopaedics are the first specialties. Initial personas include clinic owner, administrator, receptionist, doctor, nurse, cashier, radiology technician, lab technician and patient. One centrally operated deployment serves multiple independent clinic customers, each modeled as an isolated tenant that normally has fewer than 10 branches and 1–20 staff in a typical clinic. A clinic may submit its onboarding form, or a platform operator may register it directly; platform approval is always required before activation. Only platform operators manage the configurable document-type catalog. Initial sizing targets 10 clinic tenants in year one and 50 by year three, up to 100,000 registered patients and 100 appointments/day for a normal clinic tenant, and 2,000 appointments/day plus roughly 20 times the normal data volume for the owner's multi-clinic tenant. Authorized branch administrators manage allow-listed branch branding, numbering and communication-identity overrides themselves without BOOKDOC platform approval. Provider or legal verification can still gate activation of domains and communication identities. Remaining launch details are tracked in [Pre-development Decisions and Questionnaire](docs/23-pre-development-decisions-and-questionnaire.md).

### Clinic MVP — build first

1. Organization, clinic, branch, room, schedule, user, role, and permission administration.
   Every permitted role can receive a permission-composed dashboard in Admin, Portal, or both.
2. Stakeholder-based person/corporate registry; Patient is a role for a person stakeholder. Shared identifiers, contacts, addresses and documents are stored once, with patient relations, consent and duplicate detection layered above it.
3. Provider/staff profiles, specialties, services, availability, leave, and slot generation.
4. Appointment booking, reschedule, cancel, waitlist, check-in, queue, consultation, and checkout.
   Booking covers categorized resources—not only OPD doctors—including rooms, beds/day-care chairs, imaging modalities, equipment and service teams.
5. Encounter record: complaints, history, vitals, diagnosis, notes, procedures, prescriptions, investigations, and follow-up.
6. Clinic billing: price lists, invoices, discounts with approval, receipts, refunds, payment allocation, and daily closing.
7. Notifications: confirmation, reminder, cancellation, follow-up, and payment receipt through the platform queue.
   Channels include in-app, push, versioned email templates, WhatsApp and SMS; SignalR provides authorized live operational updates.
8. Operational reports, clinical summaries, audit, export controls, and basic dashboards.
   Admin manages the authorized report catalog and schedules; Portal exposes permission-approved operational reports. Normal output uses browser/PDF, with a constrained future print agent for receipts, queue slips and labels.
9. Admin/Staff web host, patient portal, and a staged MAUI mobile host.

### Hospital expansion — design for now, implement later

- Admission, transfer, discharge, bed/ward management.
- Nursing observations, medication administration, orders, care plans, and handover.
- Laboratory, radiology, pharmacy inventory/dispensing, operation theatre, emergency, insurance/TPA, and hospital billing.
- Employee/payroll database integration after its separate source schema and ownership rules are supplied.
- Do not place hospital-only columns in clinic aggregates. Future modules reference stable patient, practitioner, encounter, catalog, finance, identity, and organization identifiers.

## Architecture decision summary

- The three product applications are `BookDoc2026.Api`, `BookDoc2026.Admin` and `BookDoc2026.Portal`. `BookDoc2026.AppHost` is development orchestration, not a production product application; it also exposes the MAUI Windows target as an explicit-start development resource.
- Use the `SDTS.ERP2026` modular-monolith, host, Identity/JWT, roles/permissions, typed-client and shared-UI shape, with product namespaces `BookDoc2026.*`.
- Treat `BookDocAppointment` as evidence for contract/package, booking and payment behavior—not as the source for authentication, project structure or database design. The preservation map is in [DOC-032](docs/32-sdts-architecture-realignment-and-legacy-business-preservation.md).
- Replace the legacy Windows messaging service with API-hosted durable jobs. `BookDoc2026.Worker` is a class library containing immediate and scheduled job services; the API is their only execution host.
- Keep messaging, templates and generated documents in explicit libraries: `BookDoc2026.Messaging` uses `BookDoc2026.Templates`; `BookDoc2026.DocumentService` produces Open XML DOCX/PDF artifacts; API composes them and Worker supplies durability.
- Share error codes, safe descriptors, categories, trace metadata and recovery guidance through platform-neutral `BookDoc2026.ErrorHandling`; keep HTTP handling in API and visual handling in each web/mobile host.
- Apply the cross-cutting capability map in [DOC-034](docs/34-cross-cutting-platform-hardening.md): separate telemetry, audit and domain history; use typed scoped configuration, durable callback inboxes, explicit API compatibility and privacy-safe observability; do not create a generic catch-all platform library.
- Use .NET 10 and EF Core migrations unless a separate deployment compatibility decision changes this.
- Use `uint` CLR IDs for ASP.NET Core Identity user/role models, positive `long`/SQL `bigint` for tenant and business aggregates, and resource-kind/tenant-bound protected strings in public contracts; retain `Guid` only for explicit idempotency requests. SQL Server stores Identity unsigned values as `bigint` through EF mapping.
- Start with one SQL Server database and schema-per-module ownership.
- Keep API contracts versioned and platform neutral; Admin, Portal, and MAUI consume the same typed client.
- Admin is an ERP-style frontend restricted by approved network/IP policy and owns the main reporting, export and import management experiences. Portal is internet-facing but authenticated, permission- and tenant-scoped. Future mobile apps are focused mobile versions of Portal workflows.
- Reusable Blazor components live in `BookDoc2026.Blazor.UI`; reusable native components and device services live in `BookDoc2026.Maui.UI`. The `BookDoc2026.Mobile` MAUI Blazor Hybrid shell reuses both boundaries and the common typed Client.
- Provision the first platform operator only through the guarded `BookDoc2026.Bootstrap` command with secret/environment input; never seed or commit an operator password.
- Never copy legacy controllers, repositories, EF6 entities, MVC views, WinForms, or raw SQL directly into production.
- Build a modern schema from domain boundaries and migration maps; legacy table parity is not a design goal.
- Retire WinForms/RDLC reporting only after active reports are classified, rebuilt/merged/replaced, reconciled and accepted. The observed inventory is 45 primary layouts plus 43 export variants.
- Reuse legacy business vocabulary, validated workflows, reports, reference data, and migration mappings after tests confirm them.
- Store UTC timestamps (`datetimeoffset`) and render in the clinic/branch time zone. Scheduling also stores the applicable IANA/Windows time-zone identifier.
- Treat patient and clinical data as sensitive. Enforce least privilege, audit access and mutation, redact logs, encrypt transport/storage, and define retention/backup policy before pilot use.

## Phase gates

| Gate | Exit condition |
|---|---|
| G0 Discovery | Real production database schema and representative anonymized data are profiled; secrets rotated |
| G1 Foundation | Solution builds; architecture, authorization, tenant isolation, audit, migrations, and CI tests pass |
| G1A Platform Hardening | Correlation/redaction, typed configuration, API compatibility, deterministic time, supply-chain and restore controls have owners and passing reference evidence |
| G2 Patient + Resource Scheduling | Reception can register/search a patient and complete doctor/room/machine/bed-category booking without conflicts |
| G3 Clinical | Clinician can complete, sign, amend, and print an encounter without unsafe overwrites |
| G4 Billing | Invoice-to-receipt flow reconciles and daily close is reproducible |
| G4A Queue + Realtime | OPD and first imaging queue work end-to-end; authorized dashboards recover correctly through SignalR reconnect |
| G4B API Jobs + Reporting | API-hosted Worker retry/dead-letter passes; Admin/Portal report permissions, golden totals and scheduled-report controls pass |
| G5 Migration rehearsal | Two repeatable rehearsals pass count, money, relationship, and workflow checks |
| G6 Pilot | One clinic operates in controlled parallel/cutover with support and rollback ready |
| G7 Mobile + Channels | Approved mobile journeys, push, email and WhatsApp pass security, consent, delivery and device checks |
| G7A Operational Printing | Browser/PDF output works; paired-agent receipt/slip/label printing passes branch, duplicate and offline tests |
| G7B Clinic 100% Audit | All twenty DOC-033 rows satisfy DOC-035 implementation, acceptance and operational evidence; future hospital/payroll exclusions are explicit |
| G8 Hospital module | Only starts after clinic stability metrics and hospital requirements are approved |

## Immediate execution queue

1. Answer and approve the remaining blocking items in document 23, especially concurrent-user/message/document volume, initial Delhi document requirements and named business owners.
2. Hold Physiotherapy and Orthopaedics workflow workshops and approve the readiness gate in document 24.
3. Complete the role/permission, workflow, state-machine, terminology, Delhi applicability, UX and operational preparation gates in documents 13–22.
4. Obtain a read-only schema export and anonymized production sample; source code alone cannot prove the live database.
5. Rotate every credential found in legacy configuration and remove secrets from history/deployment packages.
6. Walk through both legacy Xamarin apps with active staff/patient representatives and assign every screen a disposition.
7. Inventory current email templates, WhatsApp/SMS consent, provider accounts and delivery callbacks; do not copy embedded credentials.
8. Classify all 45 primary legacy report layouts with named users/owners and merge or retire the 43 export variants rather than recreating duplicate templates.
9. Inventory every Windows-service message/repeat task and map it to a typed Worker handler or approved retirement.
10. Approve resource categories and the first department queue vertical slice (recommended: X-ray or CT).
11. Review and accept the implemented [Foundation reference slice](docs/26-foundation-reference-slice.md) and [SDTS architecture realignment](docs/32-sdts-architecture-realignment-and-legacy-business-preservation.md); approve a secret-backed one-time platform-operator bootstrap/deployment procedure before shared or production deployment.
12. Create the legacy-to-target mapping workbook from the database profile.
13. Review the implemented Stakeholder/Patient, Resource Catalog, Scheduling, Booking lifecycle, radiology browser journey, [Contract/Package entitlement slice](docs/46-contract-package-entitlement-reference-slice.md), [Encounter signing foundation](docs/47-encounter-revision-signing-foundation.md), [Practitioner/Workforce foundation](docs/48-practitioner-credential-and-assignment-foundation.md), [Billing foundation](docs/49-billing-invoice-payment-receipt-foundation.md) and [Cashier/Admin Billing journey](docs/50-portal-cashier-and-admin-billing-oversight.md). Next implement the policy-neutral Physiotherapy clinical-delivery foundation while obtaining workshop approval before final specialty templates/states and finance-owner approval before GST, discount, refund or close logic. Do not start with generic CRUD generation.
14. Run a migration rehearsal after every completed data-owning module.
15. Apply [DOC-034](docs/34-cross-cutting-platform-hardening.md) incrementally: begin with correlation/redaction, audit separation, injectable time and inbound/outbound idempotency in the durable messaging slice; add cache/search infrastructure only after measurements justify it.
16. Execute [DOC-035](docs/35-path-to-100-percent-achievement.md) as the score-control plan: close live-evidence decisions, prove the production-grade durable messaging slice, then advance each report row only when its implementation and acceptance evidence pass.

## Planning rules

- Every backlog item must name its module, permission, API contract, database change, audit behavior, and test evidence.
- Any scope or architecture change must update this master, its owning document, and the register in document 08.
- Estimates are engineering hours, not calendar promises. Re-estimate after G0 and after the first end-to-end vertical slice.
- Current expanded clinic web/backend/reporting/printing ROM is 3,310–5,445 engineering hours before uncertainty reserve; patient MAUI M1 adds 180–280, and staff MAUI M2/M3 adds 340–560. Employee/payroll is excluded until its database is reviewed.
- No live clinical cutover is authorized by these documents; operational, privacy, legal, backup, and recovery sign-off are separate mandatory approvals.
