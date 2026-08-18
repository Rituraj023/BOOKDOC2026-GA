# 02 — Legacy Migration Assessment

## Classification method

Each capability is assigned one disposition:

- **Migrate data + rules**: preserve validated records and behavior, rewrite code.
- **Adapt**: reuse SDTS platform capability and add clinic-specific contracts.
- **Rebuild**: old behavior is useful, but model or implementation is too coupled/unsafe.
- **Defer**: retain in legacy read-only mode until a later module.
- **Retire**: remove only after usage and retention approval.

## Feature disposition

| Legacy capability | Disposition | Clinic MVP | Easy migration | Main focus before acceptance |
|---|---|---:|---:|---|
| Users and roles | Adapt | Yes | Low | Identity IDs/password hashes may not be portable; force secure activation/reset and map permissions |
| Accommodation/location | Rebuild | Yes | Medium | Split Organization, Clinic, Branch, Room; remove ambiguous naming |
| Customer/patient demographics | Migrate data + rules | Yes | Medium | Duplicate matching, identifiers, contact normalization, consent, deceased/blocked semantics |
| Family/relation/reference | Migrate data + rules | Yes | High | Correct relationship direction and merge duplicates |
| Patient documents/pictures | Adapt | Yes | Medium | Hashing, MIME verification, malware/quarantine, access audit, storage keys |
| Employees/providers/specialties | Migrate data + rules | Yes | Medium | Separate user account, person, employee, practitioner, credential and location assignments |
| Open time/shift/day event/leave | Rebuild | Yes | Medium | Recurrence, exceptions, time zone, shared resource, effective dates |
| Booking and booking status | Rebuild around migrated data | Yes | Medium | Concurrency, idempotency, state machine, slot holds, waitlist, reschedule lineage |
| Rooms/beds/provider “object” as resources | Reclassify and rebuild | Yes/future inpatient | Low | Modern categorized resource/capacity model; bed reservation is distinct from occupancy |
| Department/imaging queues | New implementation | Yes, staged | None | Service points, ticket stages, priority/SLA, modality worklist and SignalR |
| Walk-in and booking requests | Rebuild | Yes | Medium | Unified appointment source and queue behavior |
| Procedures/services/activities | Migrate catalog | Yes | High | Normalize into service catalog, duration, tax, price and practitioner eligibility |
| Medical history | Migrate data | Yes | Medium | Controlled codes vs free text, provenance, sensitivity, encounter linkage |
| Assessments/vitals/physio | Rebuild around migrated data | Yes/selected specialty | Low | Versioned clinical templates, units, abnormal flags, signing/amendment |
| Prescriptions/drug registers | Rebuild around migrated data | Yes | Low | Medication dictionary, dose/route/frequency/duration, allergies, interaction responsibility, immutable issue |
| Medical tests | Rebuild catalog/order model | Basic | Low | Separate catalog, order, specimen/result, attachments and status |
| Contracts/packages/tariffs | Rebuild selectively | Maybe | Low | Clarify active clinic use; effective-dated prices and entitlement/consumption rules |
| Invoice and payment | Rebuild around migrated data | Yes | Medium | Immutable numbering, tax snapshots, allocations, refunds, reversals, daily close |
| Coupons/adjustments/pay modes | Rebuild selectively | Should | Medium | Approval permissions and auditable reason codes |
| Reports | Re-specify then rebuild | Yes | Low | Stored procedure calculations must receive golden-result tests |
| Messaging service/utilities | Replace with durable queue + API-hosted Worker library | Yes | Low | API commands/monitoring/execution; provider adapters, consent, templates, retries, idempotency and opt-out |
| Email templates/WhatsApp | Inventory then rebuild | Yes | Medium for content only | Versioned approved templates, safe variables, provider IDs, consent and signed webhooks |
| Windows messaging service | Replace with API-hosted Worker-library services | Yes | Low | Typed handlers, durable leases/retry/dead-letter, no direct polling or arbitrary repeat SQL |
| RDLC report library | Validate, rebuild/merge/replace/retire | Yes, active set | Low | 45 primary layouts, 43 export variants, golden results and owner approval |
| WinForms reporting/printing | Retire after parity | No | None | Admin manages reports; Portal receives permitted operational reports; browser/PDF plus constrained print agent |
| Other WinForms application/installers | Retire after workflow parity | No | None | Keep only where a separately approved legacy workflow still lacks target parity; do not preserve the reporting shell twice |
| Staff Xamarin app | Re-specify journeys, rebuild in MAUI | Staged | Low | Preserve agenda/patient/clinical/task/collection workflows; replace transport, auth, state and navigation |
| Patient Xamarin app | Re-specify journeys, rebuild in MAUI | Yes | Medium for UX requirements | Preserve discovery/booking/history/profile flows; replace guest security, auth, contracts and UI |
| Admission/ward/room/bed | Defer, redesign now at boundary | Future | Low | Hospital requirements, transfers, occupancy, nursing and billing are incomplete |

## What is relatively easy to migrate

“Easy” means the data is structurally simple after profiling; it does not mean copy the old class.

1. Stable lookup catalogs: nationality, occupation, blood group, specialty, units, room types, drug route/frequency, payment modes.
2. Organization contact/setup values after ownership and naming cleanup.
3. Procedure/service descriptions and basic pricing seeds after deduplication.
4. Patient relations and referral sources after foreign-key validation.
5. Historical document metadata when the binary exists and a SHA-256 hash can be calculated.
6. Historical immutable status/reference rows with an explicit legacy ID map.

## What requires the most engineering focus

1. **Scheduling correctness** — simultaneous booking, shared providers/resources, recurrence exceptions, duration, time zones, reschedule/cancel lineage, and no-show/queue states.
2. **Patient identity** — deterministic search plus reviewed merge; never auto-merge solely by name/mobile.
3. **Clinical record safety** — signed records are immutable; corrections are amendments with author, reason, and timestamp.
4. **Money** — preserve invoice and receipt totals as imported snapshots; prove allocations and refunds down to currency precision.
5. **Tenant/branch isolation** — every scoped query and identifier must be server-validated.
6. **Legacy database dependencies** — inventory all procedures, views, functions, and triggers from the live database, not only the four SQL files in source.
7. **Reporting parity** — business formulas hidden in stored procedures need golden datasets and named owners.
8. **Credential/security remediation** — rotate exposed credentials, re-onboard accounts, and prevent sensitive values in logs/audit payloads.
9. **Mobile security replacement** — remove plain HTTP, embedded shared secrets, device-ID login, password-bearing session models, fixed user attribution and client-only field hiding found in both Xamarin apps.
10. **Worker parity** — every active legacy message/repeat task needs a typed target handler, idempotency rule, retry/dead-letter behavior and controlled parallel-run evidence.
11. **Report parity** — validate 45 primary layouts, merge/retire duplicate variants, reproduce golden totals and preserve issued-document snapshots.

## Clinic MVP use-case backlog

### Foundation

- Manage organization, clinic, branch, rooms, business hours, numbering, locale/time zone.
- Manage users, roles, permissions, scope grants, MFA/activation policy, and audit review.
- Manage notification templates, document categories, feature flags, and integration settings.

### Patient Registry

- Register temporary or full patient; upgrade temporary record without creating a duplicate.
- Search by patient number, name, mobile, email, DOB, or legacy ID with permission-aware masking.
- Add contacts, addresses, relations, emergency contact, consent, alerts, referral, documents.
- Suggest possible duplicates and complete an authorized merge with reversible lineage.

### Scheduling and Front Desk

- Configure practitioner/service/resource availability and exceptions.
- Search slots, hold slot briefly, book with idempotency key, reschedule, cancel, waitlist.
- Check in walk-in/booked patient, manage queue, mark no-show, start/finish visit, follow-up.
- Send confirmation/reminder/change notifications and keep delivery status.

### Clinical Encounter

- Open encounter from appointment or authorized walk-in.
- Capture complaint, history, allergies, vitals with units, examination, diagnoses, procedures, plan, investigations, prescription, follow-up.
- Save draft, sign, print/share summary, amend signed note; audit every action.
- Specialty templates extend a common encounter without altering core tables for every form field.

### Billing

- Create estimate/invoice from rendered services; snapshot description, tax and price.
- Approve discount, receive split/tendered payments, allocate, refund/reverse, print/share receipt.
- Daily collection close with discrepancy reason and immutable close record.

## Acceptance evidence required from legacy users

For each migrated workflow, capture:

- actor and permissions;
- starting state and sample identifiers;
- happy path and at least two exception paths;
- calculation/state transition expected;
- required print/message/report output;
- legacy screenshot/result for comparison;
- product-owner acceptance and any deliberately changed behavior.

## Not in clinic MVP unless explicitly approved

- Full inpatient/hospital operations, pharmacy inventory, lab analyzer integration, radiology/PACS, operation theatre, emergency/triage, insurance claim processing, payroll/HR, procurement, accounting general ledger, and advanced analytics.
- These remain architectural extension points, not half-built MVP screens.
