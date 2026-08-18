# 10 — Role Dashboards, Resource Booking, Queues and Realtime Messaging

Status: approved planning direction; detailed workflow validation pending  
Reviewed: 2026-08-09

## Confirmed product decisions

1. Every role may have a dashboard in both Admin and Portal when its permissions allow access.
2. The new database is a modern domain model. It will not reproduce the legacy BookDoc schema table-for-table.
3. Employee and payroll integration remains a bounded future workstream until the source database and rules are supplied.
4. Scheduling is not limited to doctor OPD appointments. It supports categorized bookable resources, including beds, rooms, imaging machines and other capacity-constrained resources.
5. Queue management covers OPD and departments such as X-ray, CT, MRI, laboratory, pharmacy, billing and other service points.
6. Live operational information uses SignalR. Mobile/background alerts use push notifications. Email and WhatsApp use versioned templates and the reliable messaging platform.
7. The legacy Windows messaging service is replaced by API-managed durable work executed inside API by services from the `BookDoc2026.Worker` class library.
8. Admin manages reports and schedules; Portal exposes permission-approved operational reports. See [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).

## Permission-composed dashboards

Admin and Portal are hosts, not hard-coded role silos:

- **Admin** is an ERP-style frontend for configuration, security, master data, monitoring, report/import/export management and management dashboards. It additionally requires an approved network/IP path.
- **Portal** is internet-facing after authentication and normally contains daily clinical, operational and patient self-service workspaces.
- A role can access either or both hosts only when it has the corresponding host and resource permissions.
- The server returns only authorized dashboard descriptors and data. Hiding a card in the browser is not authorization.

### Dashboard composition model

```text
Authenticated user
  -> durable tenant/organization/branch grants
  -> role permissions + optional direct policy grants
  -> allowed host(s)
  -> allowed dashboard(s)
  -> allowed cards/actions/data projections
```

Each dashboard definition includes:

- stable code, display name, host placement and route;
- required permission and allowed scope levels;
- role/persona recommendation, but no hard-coded role name dependency;
- ordered cards/widgets, layout and responsive breakpoints;
- card data-source/query contract, refresh mode and cache duration;
- optional SignalR topic for live refresh;
- drill-down route and command permissions;
- data-classification and masking policy.

Suggested roles/personas and dashboards:

| Persona | Portal dashboard examples | Admin dashboard examples |
|---|---|---|
| Reception/front desk | arrivals, queue, appointments, requests, bed/resource availability | schedule exceptions, service-point setup if allowed |
| Doctor/practitioner | today’s agenda, waiting patients, unsigned notes, results requiring review | practitioner/service configuration if allowed |
| Nurse | assigned patients, observations due, medication/tasks later | nursing setup later |
| Radiology technician | modality worklist, waiting/in-progress scans, machine status | modality/resource setup and utilization |
| Lab technician | specimen/work queue and overdue items | investigation/service-point setup |
| Pharmacist | prescription/dispense queue later | medicine/inventory setup later |
| Cashier | unpaid invoices, collection session, receipt/reversal alerts | payment method/till configuration if allowed |
| Clinic manager | occupancy, queues, no-shows, revenue and service levels | branch, catalog, workforce assignment and reports |
| Security/tenant admin | limited operational summary if granted | users, roles, scopes, audit, integrations, jobs |
| Patient | own appointments, queue status, released results, bills | no Admin access unless separately employed and granted |

### Dashboard permissions

Use explicit permissions such as:

- `Hosts.Admin.Access`, `Hosts.Portal.Access`
- `Dashboards.FrontDesk.View`, `Dashboards.Clinical.View`, `Dashboards.Radiology.View`
- `Dashboards.Finance.View`, `Dashboards.Management.View`, `Dashboards.Security.View`
- Card actions still require their business permission, such as `Appointments.CheckIn` or `Queues.CallNext`.

## Generalized resource booking

### Resource categories

The Catalog/Resource modules define hierarchical categories and resource types rather than special-casing doctors:

| Category | Examples | Capacity rule |
|---|---|---|
| Practitioner | doctor, therapist, technician | one or configured concurrent patients |
| Space | consultation room, procedure room, theatre | capacity and compatible service |
| Bed | ward bed, ICU bed, day-care chair | reservation then inpatient occupancy |
| Imaging modality | X-ray unit, CT scanner, MRI, ultrasound | duration, preparation, maintenance and room dependency |
| Equipment | ECG, dialysis machine, therapy equipment | quantity or individual serialized resource |
| Team/service point | vaccination desk, lab collection, pharmacy counter | pooled capacity and queue stages |

A service defines its required resource roles. For example, a CT appointment may require a CT scanner, imaging room, technician pool and optional radiologist; the booking transaction reserves the constrained combination.

### Booking aggregate

- `booking` represents a scheduled service request for a patient/customer and a time interval.
- `booking_resource` reserves one or more concrete resources or capacity pools.
- `booking_category` and service rules determine duration, preparation, dependencies, cancellation and priority policies.
- `availability_rule`, `availability_exception` and maintenance downtime apply to any resource.
- The server performs conflict checks for every constrained resource in one transaction.
- A booking may originate from self-service, staff scheduling, a clinical order, an admission plan or a waitlist offer.

### Bed distinction

Bed scheduling and bed occupancy are related but not identical:

- Scheduling may create a **bed reservation** with expected start/end, category and requirements.
- The future Inpatient module creates the authoritative **bed stay/allocation** at admission/check-in.
- Transfer, discharge, isolation, cleaning and maintenance change actual bed state.
- A reservation never proves current occupancy, and a bed cannot be double-allocated.

## Queue management

Queue management is a reusable operational module, not an appointment status field.

### Core concepts

- **Service point**: OPD room, X-ray, CT, MRI, specimen collection, pharmacy, cashier, etc.
- **Queue definition**: operating hours, token format, priority policy, stages and SLA thresholds.
- **Queue ticket**: patient, service/order/booking context, arrival/check-in, priority and current state.
- **Queue stage**: waiting, called, accepted, preparation, in service, paused, completed, skipped, cancelled.
- **Queue assignment**: service point/resource/operator handling the ticket.
- **Queue event**: append-only state transition with actor, reason and timestamp.

### Imaging workflow

```text
Clinical order
  -> authorization/preparation check
  -> scheduled or walk-in registration
  -> arrival/check-in
  -> modality queue
  -> called/preparation
  -> scan started/completed
  -> quality check
  -> reporting queue
  -> report verified
  -> result released according to policy
```

X-ray, CT, MRI and ultrasound share worklist/queue mechanics but retain modality-specific preparation, safety, contrast, protocol and reporting data. DICOM images belong in PACS/VNA integration; BOOKDOC2026 stores study identifiers, status, report metadata and authorized links, not large diagnostic image sets in the generic document table.

### Queue rules

- Priority is controlled by policy and permission, with reason and audit; emergency priority cannot be a hidden client value.
- Appointment time, arrival time, clinical urgency and resource availability may influence ordering.
- Calling a ticket uses optimistic concurrency so two operators cannot take the same patient.
- Skips, recalls, transfers and cancellations create events; they do not erase history.
- Public displays use a privacy-safe token, never patient name or clinical details unless policy explicitly permits it.
- SLA timers and waiting-time metrics use server timestamps and account for paused/blocked states.

## SignalR live information

Use SignalR for transient live updates; the database remains the source of truth.

### Event families

- appointment/request/slot availability changed;
- patient checked in or queue ticket state changed;
- resource/bed/modality availability or downtime changed;
- clinical order/result status changed;
- invoice/payment/collection status changed;
- notification delivery status changed;
- dashboard metric/card invalidation.

### Security and reliability

- Authenticate the hub and authorize every subscription from durable scope and permissions.
- Server assigns groups such as tenant, branch, service point, user and patient-self; clients cannot freely join arbitrary groups.
- Send minimal event envelopes: event ID/type, entity ID, version, scope and timestamp. Do not broadcast clinical text, phone numbers, money details or document content.
- On event receipt, the client fetches an authorized current projection.
- Handle reconnect, missed events and out-of-order delivery by comparing versions and refreshing from API.
- SignalR is not the audit log or guaranteed delivery channel. Persist domain/outbox events separately.
- SignalR may invalidate dashboard/report/print status, but background execution remains in the durable Worker and status/content is retrieved through authorized APIs.
- Add a supported backplane/service before multiple API instances require cross-node fan-out.

## Push notifications

- Use a provider abstraction with platform adapters for Android and iOS.
- Register device installation, platform, push token, user, app, last-seen and revocation state.
- A user can have multiple devices; logout/revocation removes or disables the installation.
- Push payloads are generic and contain an opaque notification/event ID plus safe deep-link hint.
- Opening a notification requires normal authentication and authorization before details load.
- Provider failures update delivery attempts and invalid tokens are retired.
- User preferences, consent, quiet hours, clinical urgency exceptions and locale/time zone are server controlled.

## Template-based email and WhatsApp

### Unified template model

Email, WhatsApp, SMS, push and in-app messages use a shared template registry with channel-specific content:

- template code, purpose, channel, locale, tenant/organization override;
- version, draft/approved/retired state and effective dates;
- subject/title, text body, HTML body, provider template ID and parameter schema;
- allowed variables with types and sensitivity classification;
- preview/test-send, approval actor/time and immutable published versions;
- opt-in/opt-out category, quiet-hour and retention behavior.

Never execute arbitrary template code. Render only allow-listed variables using a safe engine; escape HTML by default and validate required parameters before enqueue.

The same approved templates and messaging queue deliver scheduled-report notifications and secure expiring download links. Sensitive reports are not attached to email or WhatsApp by default.

### Email modernization

- Responsive branded HTML plus meaningful plain-text fallback.
- Central layout and reusable header/footer without copying full templates.
- Absolute approved links, expiring signed actions where needed, no passwords or sensitive clinical data.
- Provider delivery, bounce and complaint webhooks are signature-verified and idempotent.
- Preview across major clients and accessibility checks before publishing.

### WhatsApp messaging

- Integrate through an approved WhatsApp Business provider using `IMessageDeliveryProvider`.
- Outbound business-initiated messages use approved provider template IDs and exact parameter definitions.
- Record patient/customer consent, source, purpose, timestamp and withdrawal; enforce opt-out.
- Inbound webhook signatures, replay protection and idempotency are mandatory.
- Store provider message ID, status transitions and failure codes without logging message bodies or access tokens.
- Define escalation/handoff if two-way conversations are enabled; do not silently turn a notification channel into unstaffed clinical chat.

## Employee and payroll boundary

Until the employee/payroll database is supplied:

- Workforce owns only staff/practitioner identity, credentials, branch/service/resource assignments and scheduling attributes required for clinic operation.
- Payroll amounts, attendance calculation, leave balances, salary structures, statutory deductions and postings are not designed or migrated yet.
- Use external/legacy employee IDs and an integration map, not copied payroll columns in clinical tables.
- When supplied, profile the database and decide authoritative ownership field-by-field through an anti-corruption/integration layer.
- Do not make payroll availability a prerequisite for clinic MVP unless a concrete workflow proves it.

## Acceptance outcomes

- A permitted user sees the correct role dashboard in Admin, Portal or both; unauthorized cards, data and routes are inaccessible server-side.
- A booking can atomically reserve the required practitioner/room/machine/bed-category resources without overlap.
- A bed reservation converts to authoritative inpatient allocation without double occupancy.
- X-ray/CT and other service queues support check-in through completion with audited priority, transfer and SLA timing.
- Portal dashboards update quickly through SignalR but recover correctly after disconnect/missed events.
- Push, email and WhatsApp are queued, consent-aware, template-versioned, retryable and traceable without leaking sensitive payloads.
