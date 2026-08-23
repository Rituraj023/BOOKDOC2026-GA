# 05 — Migration and Test Plan

## Migration strategy

Use a strangler-style module migration with repeatable ETL and a controlled cutover. Do not connect new EF entities directly to legacy tables for normal operation, and do not perform a one-off manual copy.

```text
Legacy SQL (read only)
  -> Extract snapshot + watermark
  -> Raw staging tables/files
  -> Profile and quarantine invalid rows
  -> Transform to canonical import records
  -> Load target module transactionally in batches
  -> Reconcile and publish signed report
  -> Repeat until cutover
```

## Migration assets to create

Place code in a dedicated `BookDoc2026.Migration` tool and tests in `BookDoc2026.Migration.Tests`.

- Source schema/profile command.
- Versioned mapping configuration for every source table/column/code.
- Extractors with a consistent snapshot or explicit watermark.
- Staging records that preserve `source_system`, `source_table`, `legacy_id`, extraction batch, and source hash.
- Transformers that are deterministic and independently testable.
- Loaders using target application/import ports, not UI endpoints.
- Durable legacy ID maps per aggregate.
- Quarantine output with reason codes and no secrets/clinical content in logs.
- Reconciliation queries and machine-readable plus human-readable reports.
- Resume/checkpoint behavior and idempotent reruns.

## Data waves

| Wave | Data | Dependency | Key validation |
|---:|---|---|---|
| 0 | organizations, branches, settings, code maps | none | approved scope/time zone/currency |
| 1 | users as activation invitations, roles/permissions | Wave 0 | grants and login reset; no password copying unless proven safe |
| 2 | lookup catalogs, rooms, specialties, services, price references | Wave 0 | code dedupe and effective dates |
| 3 | patients, identifiers, contacts, relations, consents, alerts | Waves 0–2 | duplicate and orphan review |
| 4 | practitioners, employees, assignments, availability | Waves 0–2 | active branch/service mapping |
| 5 | appointment requests, appointments/status history/waitlist candidates | Waves 3–4 | request-vs-booking classification, time conversion and valid state lineage |
| 6 | encounters, history, assessments, vitals, prescriptions, tests, documents | Waves 3–5 | ownership, signing classification, file hash |
| 7 | invoices, lines, payments, allocations, refunds | Waves 2–6 | currency and control totals |
| 8 | report projections and archived/deferred inpatient data | all relevant | report golden totals and retention |

## Record disposition

Each source row must end in exactly one state:

- loaded with target ID;
- intentionally coalesced with named target ID and rule;
- quarantined with actionable reason;
- deferred with approved module/release;
- excluded with approved retention/business reason.

Silent row loss is a release blocker.

For Wave 4, practitioner and employee rows receive separate dispositions. A legacy doctor/user is mapped to one Person Stakeholder, optionally one Identity invitation and one Practitioner profile; login IDs, doctor records and employee/payroll rows are never collapsed by assumption. Registration authority/number/validity evidence maps to a credential only after source provenance is known. Branch and service eligibility maps to effective Practitioner assignments, while doctor-only appointment objects map separately to Practitioner-kind resources when scheduling evidence requires them.

## Time and identity conversion

- First identify whether each legacy datetime represents UTC, server local, clinic local, date-only, or unknown.
- Convert known instants to UTC `datetimeoffset` and retain source text/value in staging evidence.
- Flag DST-invalid or ambiguous timestamps. Do not guess silently.
- Normalize phone/email into search columns; preserve original display values.
- Generate target patient/appointment/invoice numbers only according to approved preservation policy. Historical numbers usually remain visible as legacy references.

## Financial reconciliation

For each branch and period compare:

- invoice count, subtotal, discount, tax, total, void amount;
- payment/receipt count and amount by method/status;
- allocation total, unallocated payment, outstanding balance;
- refund/reversal count and amount;
- daily/monthly report totals against approved legacy results.

Use decimal comparisons at currency precision; every variance has a row-level drilldown and disposition. A net-zero total is insufficient if individual accounts are wrong.

## Migration rehearsals

1. **Developer sample** — synthetic plus anonymized edge cases; prove mechanics.
2. **Full rehearsal 1** — production-sized snapshot; discover quality/performance issues.
3. **Full rehearsal 2** — repeat from clean target using frozen scripts; prove repeatability and runbook timing.
4. **Dress rehearsal** — production-equivalent infrastructure, operators, monitoring, backup/restore, delta window, reconciliation, rollback decision.
5. **Cutover** — freeze legacy writes or run an approved delta strategy; migrate, reconcile, authorize go/no-go, switch traffic.

## Windows-service migration and retirement

The legacy Windows messaging service remains unchanged while active email, SMS, WhatsApp and repeat-job behavior is inventoried. Each active task receives an owner, typed target handler, trigger, scope, idempotency key, retry policy, dead-letter behavior and monitoring rule. Arbitrary repeat SQL or stored-procedure text is not copied into the target.

1. Disable or owner-retire inactive tasks in the legacy inventory.
2. Shadow the new Worker with delivery suppressed and compare due-task selection, template version, recipient policy and expected outcome.
3. Parallel-run one channel/task family at a time with mutually exclusive ownership so both systems cannot deliver the same message or execute the same job.
4. Reconcile queued, succeeded, retried, dead-lettered and cancelled counts; investigate every duplicate or missed outcome.
5. Enable production durable execution through the Worker library hosted inside API. The API owns commands, permissions, monitoring, administration and background execution.
6. Stop and retain the legacy service in rollback-ready state for the approved observation period, then remove its startup/deployment only after owner and operations approval.

SignalR may publish privacy-safe status invalidations, but it never executes or guarantees a durable job.

## Report and printing migration

Inventory the **45 primary RDLC layouts and 43 export variants** with named business owners and recent, privacy-safe usage evidence. Assign each primary layout and variant `Rebuild`, `Merge`, `Replace` or `Retire`. An unused report is not retired solely because telemetry is absent; its owner must approve the disposition and retention/access consequences.

For every active report:

- define parameters, host and report permissions, durable tenant/organization/branch scope, formulas, columns, sorting and output formats;
- create a fixed golden dataset containing normal, zero, boundary, cancelled/reversed and multi-branch cases;
- compare row membership, aggregates, rounding, date/time boundaries and redaction with the accepted legacy result;
- obtain user acceptance for intentional differences and preserve signed evidence;
- verify Admin management ownership and Portal access only for permission-approved operational reports;
- determine whether output is regenerated or retained as an immutable snapshot under the approved document policy.

Normal output uses browser print or PDF. The future local print agent is introduced only for receipts, queue slips and labels after browser/PDF acceptance. Agent rollout is branch-by-branch with registered printers, short-lived claims, payload hashes and auditable acknowledgements. RDLC/WinForms retirement requires accepted active-report parity, approved merge/retirement decisions, historical snapshot accessibility and no remaining scheduled or print dependency.

## Cutover runbook

### Before

- Change freeze, approved deployment artifacts and hashes, tested backup/restore, capacity and monitoring checks.
- Notify users/support; publish downtime and escalation contacts.
- Verify legacy snapshot/watermark and target is empty or at expected baseline.
- Confirm rollback decision owner, time limit, and criteria.

### Execute

1. Stop or fence legacy writes.
2. Take final backup and record source database/schema hashes.
3. Extract delta/final snapshot.
4. Run ordered migration waves with checkpoint evidence.
5. Run automated reconciliation and critical workflow smoke tests.
6. Obtain business/technical go-live approval.
7. Enable new hosts gradually and monitor errors, latency, queues, money totals, and login failures.

### Rollback

- Stop new writes immediately.
- If returning to legacy, export/handle any new-system transactions according to the pre-approved rollback bridge; never discard clinical or financial writes.
- Restore routing and verify legacy integrity.
- Preserve failed target database/logs for analysis under access controls.

Prefer forward-fix after irreversible new clinical/financial writes unless the rollback bridge has been rehearsed.

## Test pyramid and ownership

| Layer | Purpose | Examples |
|---|---|---|
| Domain unit | invariant/state logic | appointment transitions, overlap policy, signing, invoice rounding |
| Application unit | use-case behavior with ports | permission-independent orchestration, idempotent handling, error codes |
| Infrastructure integration | real SQL behavior | mappings, constraints, query filters, transactions, concurrency, migrations |
| API integration | complete HTTP pipeline | auth, forged scope, validation, conflict, versioning, file limits |
| Contract/client | protocol stability | serialization, error mapping, UTC, token refresh, OpenAPI compatibility |
| Component/UI | interactive behavior | form validation, permission visibility, stale-state recovery |
| Browser/device E2E | critical journeys | patient registration through payment; mobile booking/check-in |
| Migration | correctness/repeatability | mapping, quarantine, rerun, reconciliation, performance |
| Nonfunctional | operational safety | load, security, accessibility, backup/restore, offline sync |
| Worker/jobs | durable background behavior | claim leases, retries, dead letters, idempotency, process restart |
| Reports/printing | data and output parity | golden results, authorization, snapshots, printer-agent delivery |

## Mandatory test scenarios

### Scope and security

- User cannot read/write another tenant, organization, or unauthorized branch by ID, filter, export, document owner, or forged header.
- Missing permission produces 403; missing/invalid authentication produces 401.
- Logs/audit omit passwords, tokens, secrets, clinical body text, message destination/body, and document bytes.
- File upload rejects size/type/signature violations and quarantines until scan result.
- OWASP-focused tests cover injection, broken object authorization, mass assignment, rate limits, and unsafe redirects.
- Clinic applicants can submit only their own application/files and cannot approve, activate or view another application; operator-created applications still require an explicit authorized activation decision.
- Tenant users cannot create or activate platform onboarding document types, and users without the branch-configuration permission cannot change a branch override.
- An authorized branch administrator can activate an allow-listed branding or numbering override without platform approval; the audit record retains actor, scope and old/new versions.
- A domain or communication identity awaiting provider/legal verification cannot be used for delivery, and duplicate/out-of-order verification callbacks do not activate the wrong version.
- Numbering changes cannot duplicate an active series or rewrite numbers/configuration snapshots on issued invoices, receipts or clinical documents.
- Onboarding and override files enforce type/signature/size/malware, expiry, replacement lineage and download authorization.

### Scheduling

- Two simultaneous requests for the final slot: exactly one succeeds.
- Retry with same idempotency key returns same result; different payload is rejected.
- Reschedule preserves old record and links new record.
- Hold expiry, cancellation, no-show, overbook permission, leave/closure exception, branch time zone and DST cases.
- Guest/quick request is rate-limited and verified, cannot claim a confirmed booking, and creates exactly one appointment only when an offered slot is accepted.
- Concurrent multi-resource booking reserves doctor/room/machine/bed category atomically or reserves nothing; pooled capacity cannot be exceeded.
- Confirmation creates one distinct Booking, immutable role-aware allocations, audit and outbox row atomically; replay returns the existing Booking, while incomplete mandatory roles leave the hold active and create no side effects.
- Bed reservation and future inpatient occupancy cannot double-allocate a bed; conversion and cancellation preserve lineage.

### Queues and realtime

- Two operators calling the same queue ticket produce one successful assignment; priority/transfer/skip requires permission and reason.
- X-ray/CT worklist follows order, arrival, preparation, scan, quality/report and release states without silently skipping required stages.
- Public queue display never exposes patient identity or clinical details.
- SignalR subscription rejects forged tenant/branch/service-point groups; events contain no sensitive payload.
- Disconnect/reconnect, duplicate/out-of-order/missed events converge by API refresh and version comparison.
- Dashboard cards and commands are independently authorized in both Admin and Portal.

### Notifications and templates

- Push tokens are user/app/device bound, revocable and removed on provider-invalid response.
- Email/WhatsApp rendering rejects unknown/missing variables, escapes unsafe content and preserves the published template version.
- Consent, opt-out, locale and quiet-hours are enforced before provider dispatch; durable outbox payloads contain no destination or message body, and a policy suppression is recorded without calling the provider. Any urgent/legal-basis exception requires separately approved rules and evidence.
- Email/WhatsApp/push webhooks verify signature and idempotency; duplicate callbacks do not duplicate state or business actions.
- Logs/audit exclude destination, tokens, message bodies, clinical content and provider credentials.
- Scheduled report delivery uses the same published email/WhatsApp template registry, consent/channel policy and secure-link rules; attachments follow explicit sensitivity and expiry policy.

### Worker and durable jobs

- API command creation is authenticated, permission-checked, scope-bound and idempotent; unauthorized callers cannot create, cancel, retry or inspect another scope's jobs.
- Worker services in two API instances cannot complete the same leased item twice; lease expiry after process failure permits safe recovery.
- Transient failures follow bounded backoff; permanent failures enter a dead-letter state with redacted diagnostics and an authorized replay path.
- Restart during email, WhatsApp, scheduled-report or repeat-job handling does not duplicate the externally visible outcome where the provider supports idempotency; otherwise reconciliation makes ambiguity explicit.
- Development and production API-hosted execution pass the same handler contract, lease and idempotency tests, including multiple API instances.
- Legacy/new parallel-run ownership prevents duplicate deliveries and produces reconciled task counts.

### Reports and printing

- Report discovery accounts for all 45 primary layouts and 43 export variants, with a disposition and owner for each.
- Host permission, report permission and durable scope are independently tested for Admin preview, scheduling, export and Portal operational access.
- Parameter tampering, forged branch IDs, hidden-column export and direct snapshot URLs cannot bypass authorization or redaction.
- Golden-result comparisons cover row membership, totals, rounding, time zones, cancellations/reversals and HTML/PDF/XLSX/CSV equivalence where supported.
- Only Admin-authorized users can create, change, pause or rerun schedules; delivery cannot broaden the report creator's approved scope.
- Required signed clinical documents, prescriptions, invoices, receipts and officially distributed reports retain immutable snapshots; operational reports are regenerated.
- Browser/PDF printing works without a local agent for ordinary reports.
- A print agent that is offline leaves jobs claimable without silent loss; reconnect does not duplicate an acknowledged receipt, slip or label.
- A duplicate claim or acknowledgement is idempotent, payload-hash mismatch is rejected, and a wrong-branch agent or printer cannot claim the job.
- Printer unavailable, paper/label error, expired claim and agent-version mismatch produce visible, retryable or terminal states according to policy.

### Clinical

- Draft concurrency conflict is visible and recoverable.
- Signed note/prescription cannot be overwritten or deleted.
- Amendment preserves prior hash/version, actor, time and reason.
- Observation units/precision and abnormal/reference behavior are tested.
- Export/print and sensitive-note access require permission and create audit evidence.

### Workforce and Practitioner eligibility

- A corporate Stakeholder, inactive Identity subject, duplicate Practitioner code/person/subject or forged cross-tenant ID cannot create a Practitioner.
- Credential verification/rejection is separately authorized, versioned and final; expired, future, pending or rejected evidence cannot activate/sign.
- Assignment service, branch and optional Practitioner-kind resource/capability are validated; overlapping active periods are rejected.
- Profile suspension/inactivation, assignment suspension/end, Identity deactivation or Stakeholder inactivation immediately blocks new signing without rewriting existing clinical signatures.
- `Encounters.Sign`/`Encounters.Amend` without current Practitioner eligibility returns 403; eligibility without the Encounter permission also returns 403.
- Multi-branch responses and mutations cannot expose or alter assignments outside durable branch scope.

### Billing

- Server rejects client-tampered totals.
- Rounding at line/invoice/tax levels matches approved examples.
- Allocation cannot exceed confirmed payment or balance.
- Duplicate payment callback does not duplicate receipt/allocation.
- Discount/refund/reversal approvals and closed-session restrictions work.

### Migration

- Every source disposition is counted; source total equals loaded + coalesced + quarantined + deferred + excluded.
- Referential and scope integrity, uniqueness, nullability, code mappings, file hashes, timestamp conversion.
- Rerunning a batch changes no already-correct result.
- Full volume completes within cutover budget and does not exhaust log/storage.

### Multi-tenant capacity and performance

- Year-one tests model at least 10 independent clinic tenants with strict isolation under concurrent API request, background-job, report and document activity.
- A normal clinic dataset includes up to 100,000 registered patients and a business day of at least 100 appointments.
- The owner-operated large-tenant dataset includes roughly 20 times normal data volume and at least 2,000 appointments in a business day across its branches.
- Patient search, appointment/resource availability, queues, dashboards and transaction entry meet approved p95 targets at normal and large-tenant load.
- One large tenant's report, notification burst, import or queue activity cannot starve normal tenants; Worker fairness and database contention are measured.
- Backup/restore, migration rehearsal, retention cleanup and report execution are timed using the large-tenant dataset, not only developer samples.

## Quality gates

Every pull request: format/analyzers, build, unit tests, architecture tests, affected integration/API tests, migration script review, package vulnerability check.

Every release candidate: full suite, OpenAPI compatibility, clean-database migration plus upgrade migration, authorization matrix, browser smoke at desktop/mobile widths, MAUI device suite when affected, Worker restart/retry/dead-letter evidence, report golden comparisons, print authorization/offline tests when affected, load baseline, backup/restore evidence, data migration/reconciliation where relevant.

## Test data

- Default to generated synthetic patients and transactions.
- Anonymized production samples require approval, documented transformation, minimum fields, restricted storage, and expiry/deletion.
- Seed named edge cases: duplicate identity, partial DOB, shared mobile, invalid legacy date, overlapping slots, multi-branch user, draft/signed amendment, split payment/refund, missing document.

## Release acceptance

A gate passes only when automated evidence is attached, known variances are approved, critical/high defects are zero, security/privacy sign-offs are complete, rollback/forward-fix is rehearsed, and named business owners accept the workflow—not merely because the build is green. Windows-service and RDLC/WinForms retirement additionally require their dedicated parallel-run, report-validation and owner-approval gates in [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).
