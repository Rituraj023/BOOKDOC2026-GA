# 04 — Database Design Step by Step

## Design principles

- SQL Server, one database initially, schema-per-module ownership.
- Application-generated positive signed `long` keys map to SQL `bigint` and remain separate from human numbers. Public DTOs and routes use tenant/type-bound protected strings; `Guid` remains only for explicit idempotency requests. See [DOC-031](31-numeric-and-protected-identifier-reference.md).
- All scoped business tables carry `tenant_id`, `organization_id`, and normally `branch_id`; enforce scope through server context and indexes.
- Store instants as `datetimeoffset` UTC. Store `date` and local `time` only when they are true business concepts; scheduling records a time-zone ID.
- Use `decimal(19,4)` for money/rates unless a narrower approved rule exists; currency is explicit.
- Use restrictive deletes. Clinical and posted financial data is never cascade-deleted.
- Mutable high-value rows use `row_version`. Common audit fields are `created_on`, `created_by_user_id`, `updated_on`, `updated_by_user_id`, `status`.
- Avoid polymorphic foreign keys for core clinical relationships. The shared documents service may use validated `module/owner_type/owner_id` because it is an explicit platform boundary.
- Do not copy legacy uppercase names or overloaded `Object`, `Customer`, `Accommodation`, `Register` concepts.

## Step 0 — Profile the source database

Before target DDL is final, export metadata for tables, columns, PK/FK, defaults, checks, unique constraints, indexes, views, procedures, functions, triggers, row counts, null rates, distinct enum values, orphan counts, and duplicate candidates. Capture a schema hash and extraction timestamp.

Deliverables:

- `legacy-schema-inventory.csv`
- `legacy-data-profile.csv`
- `legacy-code-value-map.csv`
- `legacy-to-target-column-map.csv`
- stored-object dependency graph
- data-quality issue register

No production identifiers or medical text belong in the repository.

## Step 1 — Platform and organization foundation

Adapt SDTS tables in `identity`, `audit`, `messaging`, `scheduler`, and `files`. Add:

| Schema.table | Essential columns/constraints |
|---|---|
| `platform.tenant_application` | source (`ClinicSubmitted`/`OperatorCreated`), applicant/contact, proposed clinic identity, workflow state, submitted/reviewed times and assigned reviewer; protected evidence references kept outside general logs |
| `platform.tenant_application_event` | append-only submission, verification, approval, rejection, withdrawal and provisioning history with actor/reason |
| `platform.onboarding_document_type` | configurable stable code/name, jurisdiction/clinic-type applicability, accepted file categories, required/optional rule, expiry requirement and active/version state |
| `platform.tenant_application_document` | application, document-type/version, secure file reference, issuer/reference, issue/expiry dates, review state, reviewer/time/reason and replacement lineage |
| `platform.tenant` | immutable tenant ID, code/display name, lifecycle state, region/plan metadata, provisioned/closed times; unique code |
| `platform.tenant_domain` | tenant FK, verified domain/host name, purpose and active state; unique normalized host name |
| `platform.tenant_support_access` | requester/approver/operator, reason, permitted scope, starts/expires/revoked time and audit correlation |
| `platform.configuration_change_request` | tenant/branch target, allow-listed change type, proposed version/payload hash, requester/reviewer, state, reason and approval/activation times |
| `org.organization` | `id`, tenant, code/name, legal/tax data, default time zone/currency, default branding/communication-profile references; unique tenant+code |
| `org.branch` | organization FK, code/name, time zone, address/contact, optional branding/communication overrides, active; unique organization+code |
| `org.room` | branch FK, code/name/type, capacity, active; unique branch+code |
| `org.number_sequence` | scope, document_type, prefix, next_value, reset_policy, row_version; unique scope+type |
| `org.setting` | scope, typed key/value, effective dates; unique scope+key+effective_from |

Seed permission resources and a development-only bootstrap administrator through secure configuration, never a committed password.

Onboarding document types are dynamic catalog data, not dynamic database columns or executable rules. Only a platform operator may create, version, activate or retire document types. Uploaded content uses the shared secure document service with MIME/signature validation, malware/quarantine state, content hash, access audit and retention. A document-type change is versioned so existing approvals retain the rule that was reviewed.

Authorized branch administrators may activate allow-listed branch configuration changes, including branding and numbering, without BOOKDOC platform approval. Each change stores actor, timestamp, prior version, new version/hash and effective date. Sequence changes must pass uniqueness and issued-document immutability checks. Domains, email senders, WhatsApp numbers and SMS identities may remain pending until the applicable provider or legal verification succeeds; verification evidence and callbacks are versioned without giving the platform operator a routine approval role.

Initial planning uses one SQL Server database with shared schemas and mandatory tenant columns/keys. Database-per-tenant is not assumed. The reference slice must prove query, uniqueness, cache, storage, job, report and backup isolation; a future placement strategy may move large/regulatory tenants without changing domain identifiers. Performance and restore testing includes a normal tenant with up to 100,000 registered patients and about 100 appointments/day, plus an owner-operated tenant at roughly 20 times normal data volume and about 2,000 appointments/day across branches.

## Step 2 — Stakeholder and Patient Registry

| Schema.table | Purpose and important columns |
|---|---|
| `stakeholder.stakeholder` | tenant-scoped party identity, discriminator Person/Corporate, display name, status and version |
| `stakeholder.person` | one-to-one person subtype: title/name, DOB/estimated DOB and administrative sex |
| `stakeholder.corporate` | one-to-one corporate subtype: legal/trade name and registration number |
| `stakeholder.identifier` | stakeholder FK, system/type/value, issuer and active state; unique issuer+type+normalized value |
| `stakeholder.contact_point` | stakeholder FK, type, display/normalized value, primary and verified flags |
| `stakeholder.address` | stakeholder FK, dynamic address type, address fields and one primary address constraint |
| `stakeholder.document_reference` | stakeholder FK, dynamic document-type ID, secure file ID, reference/issue/expiry metadata; never binary content |
| `patient.patient` | patient role referencing exactly one Person stakeholder; patient number, registration source/request, status, blood group and version |
| `patient.related_person` | patient FK, name/contact and optional linked patient FK |
| `patient.relationship` | patient FK, related person FK, relationship code, emergency/guardian flags |
| `patient.consent` | patient FK, consent type, version, state, captured/withdrawn time, evidence document FK/reference |
| `patient.alert` | allergy/clinical/admin alert type, severity, text/code, active dates, visibility |
| `patient.referral` | source/referrer, external reference, referred date, notes with access policy |
| `patient.merge` | surviving and merged patient IDs, reason, actor/time, reversible status |
| `patient.legacy_map` | source system/entity/legacy ID -> target patient ID; unique source+entity+legacy ID |

Indexes: normalized mobile/email; name+DOB search support; scoped patient number; active alerts; identifier uniqueness. Encrypt or tokenize selected identifiers if the threat model requires it.

## Step 3 — Workforce, catalog and resources

| Schema.table | Purpose |
|---|---|
| `workforce.practitioner` | implemented tenant-wide role linking one Person Stakeholder and one Identity subject; code, type, lifecycle and version only—no duplicated display name/contact data |
| `workforce.practitioner_credential` | implemented credential type, registration identity, authority, validity and final verification evidence |
| `workforce.practitioner_assignment` | implemented effective-dated branch/service eligibility with optional Practitioner-kind bookable resource |
| `workforce.employee` | future employment relationship referencing Stakeholder; source rules/database still required and never merged into Practitioner |
| `workforce.specialty` | future controlled specialty catalog after clinical approval |
| `workforce.practitioner_specialty` | future many-to-many specialty declaration; service eligibility is currently assignment-owned |
| `catalog.service` | clinic service/procedure, code, duration, tax category, active dates |
| `catalog.practitioner_service` | eligibility and optional duration override |
| `catalog.price_list` / `price_list_item` | effective-dated price, currency, tax, branch/payer applicability |
| `catalog.medication` | reference medication fields; source/version/provenance required |
| `catalog.investigation` | lab/imaging/other orderable reference |
| `resource.resource_category` | hierarchical category such as practitioner, room, bed, imaging modality, equipment or service team |
| `resource.bookable_resource` | category, branch, code/name, capacity mode/value, status, time zone, parent/location and row version |
| `resource.resource_capability` | resource-to-service/capability, duration/capacity override and effective dates |
| `resource.resource_dependency` | required/compatible resource relationship, such as CT scanner requiring an imaging room |
| `resource.resource_status_event` | available, unavailable, maintenance, cleaning or blocked transition with reason/time |

Do not make a user row the practitioner row. A practitioner may exist without portal access, and one user may later act in more than one context.

## Step 4 — Scheduling

| Schema.table | Purpose and constraints |
|---|---|
| `scheduling.availability_rule` | any bookable resource/capacity pool, branch, service, recurrence, local start/end, time zone, effective dates |
| `scheduling.availability_exception` | leave, closure, added session, blocked interval; range and reason |
| `scheduling.hold` | patient/service/branch interval, expiry, caller request ID, payload hash, state and version |
| `scheduling.resource_reservation` | hold, concrete resource, UTC interval and quantity; authoritative occupancy checked inside a serialized resource lock |
| `scheduling.appointment_request` | guest/patient, desired clinic/service/practitioner/date/time preferences, contact verification, source, state, expiry, idempotency key |
| `scheduling.appointment_request_event` | append-only request state, staff decision, offered slot/appointment link, reason, actor/time |
| `scheduling.booking` | number, patient, service/category, branch, UTC start/end, time zone, source/order/request, state, reason, row version |
| `scheduling.booking_resource` | booking, resource or capacity pool, required role, reserved quantity, UTC interval; constrained against overlap/capacity |
| `scheduling.booking_status_event` | append-only from/to state, reason, actor/time |
| `scheduling.booking_link` | reschedule/rebook/follow-up relationship |
| `scheduling.waitlist_entry` | desired criteria, priority, state, offer expiry |
| `scheduling.queue_entry` | appointment/encounter, token, check-in/call/start/end timestamps, priority/state |

Critical database rule: prevent active overlapping bookings for every constrained resource and prevent reserved pooled quantity from exceeding capacity. SQL Server has no simple exclusion constraint, so enforce with a serializable application transaction plus normalized occupancy/capacity records and reviewed locking. Prove practitioner, room, machine and bed races with concurrent integration tests.

Suggested indexes:

- booking `(tenant_id, branch_id, service_id, starts_at, status)`;
- booking `(tenant_id, patient_id, starts_at desc)`;
- booking-resource `(resource_id, starts_at, ends_at, status)` plus occupancy/capacity enforcement indexes;
- queue `(branch_id, operational_date, state, priority, checked_in_at)`;
- unique scoped appointment number;
- unique idempotency key within scope and operation.
- request indexes for `(branch_id, state, submitted_at)` and verified contact abuse/rate-limit review without exposing raw contact values.

## Step 4A — Operational queues

| Schema.table | Purpose |
|---|---|
| `queue.service_point` | branch department/counter/modality queue endpoint, category, hours and active state |
| `queue.queue_definition` | token pattern, stages, priority policy, SLA targets and routing rules |
| `queue.queue_stage` | ordered stage codes such as waiting, preparation, service and reporting |
| `queue.ticket` | patient, booking/order/encounter context, token, priority, state, arrival and current stage, row version |
| `queue.ticket_assignment` | ticket, service point/resource/operator and assignment interval |
| `queue.ticket_event` | append-only state/priority/transfer/call/skip event with reason and actor/time |
| `queue.daily_sequence` | scoped atomic token sequence by service point/date |

Indexes cover active worklist `(branch_id, service_point_id, operational_date, state, priority, arrived_at)`, patient history and SLA breach scans. Public display projections expose only privacy-safe token/status information.

## Step 5 — Clinical

| Schema.table | Purpose |
|---|---|
| `clinical.encounter` | patient, booking optional, practitioner, branch, type, status, started/ended/signed fields, row version |
| `clinical.encounter_participant` | practitioner/staff and role during encounter |
| `clinical.note` / `note_version` | note type, draft/signed/amended, text/structured JSON, prior version, hash, author/reason |
| `clinical.observation` | code, value type, numeric/text/code value, unit, captured time/source, abnormal flag |
| `clinical.condition` | diagnosis/problem code and original text, certainty, onset, status |
| `clinical.procedure_record` | service/procedure performed, performer/time, outcome |
| `clinical.prescription` / `prescription_version` | encounter/patient/prescriber, issued state/time, version/hash |
| `clinical.prescription_item` | medication snapshot, dose/route/frequency/duration/quantity/instructions/status |
| `clinical.investigation_order` / `order_item` | ordered tests, priority, status, requested time |
| `clinical.result` | order item, status, value/interpretation, verified actor/time; documents remain in files service |
| `clinical.follow_up` | due date/interval, service/practitioner, status, resulting appointment |

Structured JSON is acceptable only for versioned specialty-template payloads with schema name/version and indexed promoted fields. Core identity, dates, status, author, diagnosis, medication, billing link, and audit data stay relational.

## Step 6 — Billing

| Schema.table | Purpose |
|---|---|
| `billing.invoice` | implemented idempotent number, Patient, optional Booking/Contract, issue evidence, currency, server totals, allocation balance/status and version |
| `billing.invoice_line` | implemented immutable service code/name, quantity, decimal unit price and line total snapshot |
| `billing.discount_approval` | invoice/line, requested/approved actors, reason, threshold/rule |
| `billing.payment` | implemented idempotent receipt number, Patient, time, amount/currency, allocated balance, receiver and version |
| `billing.payment_tender` | implemented immutable split tender with typed method, amount and optional external reference/narration |
| `billing.payment_allocation` | implemented idempotent append-only Payment-to-Invoice amount with actor/time evidence |
| `billing.financial_document_snapshot` | implemented immutable canonical Invoice/receipt payload, schema version and SHA-256 hash; rendered storage remains future |
| `billing.refund` | payment/allocation reference, amount, reason, approval, external reference, status |
| `billing.financial_adjustment` | credit/debit/reversal with reference to original posting |
| `billing.cash_session` | branch/till/cashier, open/close values/times, discrepancy and approval |

Checks: amounts nonnegative where appropriate; allocations never exceed confirmed payment or invoice balance; totals recomputed server-side; unique scoped numbers; external provider event/idempotency uniqueness.

## Step 7 — Reporting read models

Create module-owned queries first. Add `reporting` projections only for cross-module operational reports. Refresh synchronously for essential transactional screens or through outbox consumers for dashboards. Every report defines data owner, as-of semantics, host visibility, scope, permission, filters, totals, formats and reconciliation test.

| Schema.table | Purpose |
|---|---|
| `reporting.report_definition` | code/version, owner module, Admin/Portal visibility, typed parameters, formats and permissions |
| `reporting.report_execution` | requester/scope/parameters, format, status, timing, error and temporary output reference |
| `reporting.report_schedule` | Admin-owned approved schedule, time zone, parameters, recipients, format and state |
| `reporting.report_snapshot` | immutable retained document reference, source/template versions and content hash |
| `printing.agent` | paired Windows workstation identity, tenant/branch, state and last-seen time |
| `printing.printer` | agent printer identity, allow-list state and media/capability metadata |
| `printing.print_job` | retained/temporary document, target printer, copies/media, status and idempotency key |
| `printing.print_job_event` | append-only claim, print, failure, expiry, cancellation and authorized reprint history |

Operational outputs expire and are regenerated. Signed clinical summaries, prescriptions, issued invoices/receipts and officially distributed reports retain immutable snapshots through the shared document service.

## Step 8 — Hospital extension tables (later migrations)

Use stable `patient_id`, `encounter_id`, `practitioner_id`, `branch_id`, service/catalog IDs, and billing references. Add dedicated schemas such as:

- `inpatient.admission`, `episode`, `bed`, `bed_stay`, `transfer`, `discharge`;
- `nursing.care_plan`, `observation_round`, `handover`, `medication_administration`;
- `lab.specimen`, `work_item`, `result_verification`;
- `pharmacy.stock_item`, `batch`, `movement`, `dispense`;
- `radiology.study`, `report`; external images remain in PACS, not document blobs;
- `ot.case`, `theatre_schedule`, `procedure_team`;
- `insurance.authorization`, `claim`, `claim_line`, `remittance`.

Scheduling may own a future bed reservation, but `inpatient.bed_stay` owns actual occupancy. Add an atomic conversion/link and constraints preventing two active stays for one bed.

## Step 7A — Dashboards, realtime and communications

Prefer dashboard definitions in code for initial releases; use metadata tables only for safe layout/configuration:

| Schema.table | Purpose |
|---|---|
| `platform.dashboard_preference` | user/host/dashboard layout preference; never grants access |
| `messaging.template` / `template_version` | channel/locale/scope, approved immutable subject/body/parameter schema |
| `messaging.preference` | user/patient channel consent, category, quiet hours and opt-out |
| `messaging.device_installation` | user/app/platform push token metadata, last seen, revoked/invalid state |
| `messaging.delivery_event` | provider message ID and accepted/delivered/read/failed status without secret body logging |
| `platform.outbox_message` | durable committed integration/realtime-notification source with idempotency |

SignalR connections/groups are transient and need not be primary database records. Persist only audit-relevant subscription/device/session metadata required by policy.

The API creates and monitors durable message, report and print records. API-hosted services from the Worker library claim and execute them through leases. SignalR only publishes minimal status invalidations after durable state changes.

## Migration implementation sequence

For every module:

1. Approve aggregate and table design.
2. Add entity configuration and migration.
3. Generate idempotent reviewed deployment script.
4. Add schema/constraint/index integration tests.
5. Add backward-compatible columns/tables first; backfill in bounded batches.
6. Validate counts/constraints/performance.
7. Switch application reads/writes only after rehearsal.
8. Remove obsolete compatibility structures in a later release, never the cutover release.

## Database definition of done

- Naming, types, nullability, scope, retention, and ownership documented.
- PK/FK/check/unique/index rules implemented and tested.
- Query plans meet agreed volumes; no unbounded grid/report query.
- Concurrency and idempotency tests pass.
- Backup, point-in-time recovery, restore rehearsal, encryption, least-privilege accounts, and migration rollback/forward-fix are proven.
- Reconciliation queries exist for every migrated table and every financial total.
