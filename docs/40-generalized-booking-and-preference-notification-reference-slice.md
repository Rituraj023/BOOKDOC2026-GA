# 40 — Generalized Booking and Preference-Controlled Notification Reference Slice

Owner: Scheduling + Resources + Stakeholder + Communications + Worker + security  
Status: Verified backend reference slice  
Reviewed: 2026-08-23

## Outcome

This slice converts an active Scheduling hold into a distinct, confirmed `Booking` and immutable resource allocations in one durable transaction. A Booking may reserve any approved combination of practitioner, room, bed/chair, imaging modality, equipment, team or service point; it is not a doctor-only appointment.

The same transaction writes a privacy-minimal audit record and `Scheduling.BookingConfirmed.Patient.v1` outbox message. The Worker evaluates the patient's current branch-scoped transactional email preference before it resolves the destination or calls a provider. Allowed delivery is accepted through the development provider; absent, denied, withdrawn, quiet-hours or invalid-time-zone evidence creates a terminal `Suppressed` attempt without exposing or dispatching the destination.

This is an API/Client/Worker reference slice. It does not yet provide Admin, Portal or MAUI screens and is not production authorization.

## Aggregate and responsibility boundary

- `SchedulingHold` remains the short-lived capacity claim and authoritative pre-confirmation concurrency boundary.
- `Booking` is a separate tenant-scoped business aggregate with its own protected public ID, stable `BKG-...` business number, version, status and confirmation time.
- `BookingResourceAllocation` copies every reservation from the confirmed hold, including the requirement role it satisfies. Allocations are immutable in this slice.
- `Patient`, `Service` and `Branch` are referenced by durable internal IDs. Stakeholder remains the owner of contact and preference data; Booking does not copy that data into the aggregate.
- Booking confirmation is rejected unless the hold is active, unexpired, at the expected version and all configured mandatory `ServiceResourceRequirement` roles and quantities are satisfied by compatible resources.
- A role-less reservation may be inferred only when its resource category has exactly one configured requirement. Unknown or ambiguous roles fail closed.

Confirmation, allocations, hold transition, audit and outbox creation commit atomically. Repeating the command for an already-confirmed hold returns the existing Booking without creating another Booking, allocation set or outbox message.

## Public contracts and authorization

| Operation | Permission | Result |
|---|---|---|
| `POST .../scheduling/holds/{holdId}/confirm` | `Scheduling.Bookings.Confirm` | `201 Created` for first confirmation; `200 OK` for an idempotent replay |
| `GET .../scheduling/bookings/{bookingId}` | `Scheduling.Bookings.View` | Scope-filtered Booking and allocations |

Both permissions are branch-assignable. Internal numeric keys remain server-side; DTO identifiers use the existing purpose-bound protected-string codec, including the new `Booking` public-ID kind. The business Booking number is display/search identity and is not a substitute authorization control.

## Persistence and migration

Migration `20260823092907_GeneralizedBookingConfirmation` was generated but intentionally unapplied at this checkpoint; DOC-044 subsequently records successful local-development application. Its reviewed `Up` path:

1. adds `requirement_role_code` to held resource reservations;
2. adds an alternate tenant/resource reservation key needed by the allocation relationship;
3. creates `scheduling.booking`;
4. creates `scheduling.booking_resource` with tenant-aligned foreign keys, indexes and constraints;
5. adds the Booking permissions to the permission catalog.

The `Up` path contains no table/column drop, seeded-data deletion or raw SQL. Deployment still requires a normal reviewed migration backup, rehearsal and rollback gate. The EF tools emitted a tool/runtime patch-version warning (`10.0.8` versus `10.0.10`); model validation nevertheless reports no pending changes.

## Durable notification and preference policy

The outbox payload contains IDs and approved operational values only: tenant, organization, branch, patient, service, Booking, Booking number, start/end time and time-zone identifier. It contains no patient name, email address, message body, provider token or clinical narrative.

The handler applies this order:

1. validate the typed payload and source scope;
2. detect an already terminal attempt for replay safety;
3. load the latest exact preference for branch + stakeholder + purpose + transactional + email;
4. require valid, current `Allowed` evidence and check quiet hours in the recorded time zone;
5. only then load the primary email address and render the published tenant/branch template;
6. dispatch through Messaging and persist an append-only attempt with masked recipient metadata.

The current policy is deliberately conservative. No preference evidence is not consent. `Denied`, `Withdrawn`, quiet hours and invalid time-zone evidence suppress delivery. There is no urgent/legal-basis bypass in this slice. Quiet-hours handling is terminal suppression, not delayed scheduling; durable deferral is a named next improvement.

`Suppressed` is a first-class terminal delivery status. It distinguishes an intentional policy decision from provider failure and prevents Worker retry loops. SignalR may later announce committed Booking or delivery-status changes, but it must not confirm a Booking or execute the durable job.

## Template rollout

New tenant provisioning creates and publishes the strict `Booking.Confirmed.Patient` email template for `en-IN`. Its allowed placeholders match the privacy-minimal message model.

Existing tenants are not silently backfilled by this migration because templates are tenant-owned versioned records with application-generated identities and publication history. Before enabling Booking notification processing for an existing tenant, an authorized Admin user must create, review and publish the template. Missing published template remains a visible delivery failure rather than an implicit content fallback.

## Verification evidence

Verified on 2026-08-23:

```text
Solution Release build:                    passed, 0 warnings / 0 errors
Unit tests:                                46 passed
Architecture tests:                         9 passed
Integration tests:                         31 passed
Total automated tests:                     86 passed
EF pending-model-change check:              passed (none pending)
```

Automated evidence covers:

- distinct Booking creation and immutable generalized allocations;
- stale-version and expired-hold rejection;
- mandatory category/role/quantity enforcement;
- permission denial for confirmation;
- first confirmation plus replay without duplicate durable effects;
- protected Booking lookup and branch/tenant scoping;
- allowed preference leading to an accepted, masked development delivery;
- later denied preference leading to durable suppression without provider delivery;
- incomplete mandatory-resource confirmation leaving the hold active and creating no Booking or outbox row;
- conditional SQL Server migration/transaction smoke coverage when `BOOKDOC_SQLSERVER_TEST_CONNECTION` is supplied.

The SQL smoke is skipped when that opt-in connection is absent. A deployed SQL Server rehearsal and true multi-connection race/load suite therefore remain release gates.

## Achievement impact

- DOC-033 row 14 advances from 55% to 70% because confirmed generalized Booking, role-aware resource validation, atomic persistence, authorization and replay behavior now work through API and Client.
- DOC-033 row 17 advances from 65% to 70% because preference evidence is now enforced in a real patient-addressed durable handler, with policy suppression and masked delivery evidence.
- The exact twenty-row simple average advances from 62% to **63%**.

No score is awarded for Booking UI, accepted clinic workflow, reschedule/cancel/waitlist, bed admission/occupancy, real messaging providers, scheduled quiet-hours deferral, verified-contact policy, provider fallback or production operations.

## Remaining gates and recommended next goal

[DOC-041](41-booking-lifecycle-and-waitlist-reference-slice.md) implements the recommended backend cancellation, rescheduling and waitlist-promotion slice. The next recommendation is the first imaging Queue vertical slice with API-owned transitions and SignalR status invalidation. Bed admission/occupancy remains a separate later state machine rather than being hidden inside outpatient Booking.

Do not add contract consumption or billing side effects to Booking until the legacy-derived Contract/Package entitlement and tariff rule matrix has named-owner approval. Do not select production email/WhatsApp providers until the legal, consent and vendor gates in DOC-017 and DOC-020 are approved.
