# 41 — Booking Lifecycle and Waitlist Reference Slice

Owner: Scheduling + Resources + Stakeholder + Communications + Worker + security  
Status: Verified backend reference slice  
Reviewed: 2026-08-23

## Outcome

This slice completes the first backend Booking lifecycle around the confirmed generalized Booking in DOC-040. Authorized branch users can cancel a future Booking, reschedule it through a separately acquired replacement hold, create/withdraw a waitlist request and promote a matching waitlist entry through an active hold. Every public record ID remains a purpose-bound protected string.

Cancellation, rescheduling and promotion produce privacy-minimal audit/outbox evidence. Patient lifecycle messages remain durable and are accepted or suppressed by the existing Worker preference policy before the destination is loaded.

This remains an API/Client/Worker reference slice. It does not provide role workflow screens or authorize production use.

## Lifecycle model

### Cancellation

- only a `Confirmed` future Booking may be cancelled;
- the caller supplies the expected Booking version and a 3–250 character reason;
- cancellation records time, reason, `Cancelled` status and a new version;
- Booking, audit and cancellation outbox message commit together;
- stale version, already-terminal status and cancellation after start fail closed.

Availability no longer treats every confirmed hold as permanent occupancy. A confirmed hold consumes capacity only while its linked Booking remains `Confirmed`. A cancellation therefore releases all immutable allocations without deleting or rewriting their historical evidence.

### Rescheduling

Rescheduling uses a replacement hold created through the existing resource-safe hold API. That hold must match the original tenant, branch, patient and service. In one transaction the repository:

1. locks the lifecycle operation;
2. revalidates the original Booking and replacement hold versions;
3. revalidates every mandatory resource role and quantity;
4. marks the original Booking `Cancelled` with `ReplacedByBookingId`;
5. confirms the replacement hold and creates a new Booking with `PreviousBookingId`;
6. copies immutable replacement allocations;
7. writes audit and one reschedule outbox message.

This acquire-before-release design prevents loss of the patient's current Booking if replacement acquisition fails. Repeating the same replacement-hold command returns the existing replacement and creates no duplicate effects.

### Waitlist

`BookingWaitlistEntry` stores patient, service, branch, acceptable start window, priority 1–5, reason, status and version. Its first lifecycle is:

```text
Waiting -> Promoted
Waiting -> Withdrawn
```

Promotion requires an active hold for the same patient/service whose start is inside the entry window. Hold confirmation, Booking/allocation creation, waitlist transition, audit and outbox commit together. The Booking and waitlist retain protected bidirectional lineage. A promotion replay returns the existing Booking. Withdraw uses optimistic concurrency and append-only audit evidence.

Priority is recorded for future ordered matching but does not yet authorize automatic promotion or override an operator's explicit selection.

## API and permissions

| Operation | Permission |
|---|---|
| `POST .../bookings/{bookingId}/cancel` | `Scheduling.Bookings.Cancel` |
| `POST .../bookings/{bookingId}/reschedule` | `Scheduling.Bookings.Reschedule` |
| `POST .../waitlist` | `Scheduling.Waitlist.Manage` |
| `GET .../waitlist/{waitlistId}` | `Scheduling.Waitlist.View` |
| `POST .../waitlist/{waitlistId}/withdraw` | `Scheduling.Waitlist.Manage` |
| `POST .../waitlist/{waitlistId}/promote` | `Scheduling.Waitlist.Manage` |

All permissions are branch-assignable and all queries remain tenant/branch filtered. The shared typed Client exposes the same operations; no UI host bypasses the API.

## Durable patient updates

Three new typed messages and published templates exist for new tenants:

- `Scheduling.BookingCancelled.Patient.v1` / `Booking.Cancelled.Patient`;
- `Scheduling.BookingRescheduled.Patient.v1` / `Booking.Rescheduled.Patient`;
- `Scheduling.WaitlistPromoted.Patient.v1` / `Booking.WaitlistPromoted.Patient`.

They use the exact `BOOKING.UPDATES` transactional preference. Missing, denied, withdrawn, quiet-hours or invalid-time-zone evidence records a terminal `Suppressed` attempt. Allowed evidence resolves the primary email only after policy evaluation, renders a strict template and stores masked provider evidence. Reschedule emits one message describing old and replacement Booking rather than separate cancellation and confirmation messages.

Existing tenants require authorized creation/review/publication of these three templates before lifecycle notification processing is enabled. The schema migration does not silently backfill tenant-owned template history.

## Database migrations

Two generated migrations formed the lifecycle schema step and were unapplied at this checkpoint; DOC-044 subsequently records successful local-development application:

1. `20260823095116_BookingLifecycleAndWaitlist` adds nullable cancellation/lineage fields, the waitlist table, indexes, checks and permissions;
2. `20260823095152_BookingWaitlistLinkIntegrity` adds tenant-aligned Booking/waitlist foreign keys after both sides exist.

Both reviewed `Up` paths contain no drop, seeded-data deletion or raw SQL. The first migration updates deterministic role-claim seed positions and inserts the new branch permissions; EF's generic data-loss warning was reviewed against the generated operations. Neither migration has been applied.

## Verification

Verified on 2026-08-23:

```text
API Release build:                         passed, 0 warnings / 0 errors
Unit tests:                                48 passed
Architecture tests:                         9 passed
Integration tests:                         31 passed
Total automated tests:                     88 passed
EF pending-model-change check:              passed (none pending)
```

Evidence includes domain transition/version failures, cancellation capacity release, forbidden cancel/promote/reschedule requests, stale waitlist withdrawal rejection, waitlist promotion and replay, original/replacement lineage, mandatory resource preservation, exactly three accepted lifecycle notifications and existing Booking confirmation/preference behavior.

The SQL Server smoke remains conditional on `BOOKDOC_SQLSERVER_TEST_CONNECTION`. Deployed migration rehearsal, simultaneous multi-connection lifecycle races and load/fairness tests remain mandatory gates.

## Achievement and remaining work

DOC-033 Scheduling row 14 remains **70%**. This slice closes named backend lifecycle work but does not meet the report's 75% user-outcome threshold because role UI, automatic waitlist offer/expiry, clinic acceptance, completed/no-show transitions, overbook policy, bed admission/occupancy and deployed concurrency evidence remain open. Communications row 17 remains 70% because the new handlers still use the development provider.

## Recommended next goal

[DOC-042](42-imaging-queue-and-realtime-reference-slice.md) implements the first X-ray/CT Queue backend/realtime foundation, and [DOC-043](43-radiology-admin-portal-journey.md) subsequently implements its first role-facing Admin/Portal journey. Those slices advance the exact twenty-row score from this slice's 63% checkpoint to 66.75%.

Contract/package entitlement and tariff work should not be invented from synthetic assumptions. It remains gated on the named-owner legacy rule matrix and representative database evidence.
