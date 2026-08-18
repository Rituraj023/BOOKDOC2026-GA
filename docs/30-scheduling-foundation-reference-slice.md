# 30 — Scheduling Foundation Reference Slice

Owner: Scheduling + Resources + backend  
Status: Verified reference slice  
Last reviewed: 2026-08-09

## Outcome

The first Scheduling increment supports every concrete bookable resource category—practitioner, room, bed/chair, imaging modality, equipment, team or service point. It implements planned availability, short-lived idempotent holds and authoritative resource reservations. It does not yet create a confirmed appointment/admission.

## Model and behavior

- `scheduling.availability_rule`: resource, optional service, local weekday/time, effective dates, slot interval and capacity.
- `scheduling.availability_exception`: UTC blocked interval or capacity override with reason.
- `scheduling.hold`: patient, service, UTC interval, expiry, status, request ID, SHA-256 payload fingerprint and concurrency version.
- `scheduling.resource_reservation`: one concrete resource/quantity/interval per hold.
- Availability search is advisory. Hold creation repeats all resource, capability, rule, exception and capacity checks inside the durable transaction.
- Active unexpired and future Confirmed holds consume capacity. Released and expired holds do not.
- Repeating a request ID with identical content returns the original hold; reusing it with different content is rejected.
- Holds expire after at most 30 minutes. Reads materialize expiry; a later Worker cleanup handler may sweep untouched expired holds.

## SQL Server concurrency decision

Hold creation opens a serializable transaction and acquires deterministic transaction-owned `sp_getapplock` locks for the tenant/request and each selected tenant/resource. Resources are locked in sorted ID order. Capacity is recalculated only after locks are held, then Hold, reservations and audit event commit together. This handles both exclusive and pooled resources across multiple API instances without relying on SignalR or process memory.

## API and permissions

| Route | Permission |
|---|---|
| `POST .../scheduling/availability-rules` | `Scheduling.Availability.Manage` |
| `POST .../scheduling/availability-exceptions` | `Scheduling.Availability.Manage` |
| `GET .../scheduling/availability` | `Scheduling.Availability.View` |
| `POST .../scheduling/holds` | `Scheduling.Holds.Create` |
| `GET .../scheduling/holds/{id}` | `Scheduling.Availability.View` |
| `POST .../scheduling/holds/{id}/release` | `Scheduling.Holds.Release` |

The current `NumericKeyBaseline` creates Scheduling with `bigint` entity keys and retains `uniqueidentifier` only for the hold idempotency request. It is applied to `BookDoc2026_Dev`. Automated API coverage proves availability, idempotency, overlap conflict, release and rebooking. The SQL Server smoke test executes the application-lock transaction and overlap rejection inside a rolled-back fixture.

## Next slice and gates

Implement confirmed Booking/Appointment and conversion of a valid hold in one transaction; enforce the complete service-resource requirement set; add reschedule/cancel/waitlist state machines; generate slots efficiently; add Worker expiry cleanup; add true multi-connection race and pooled-capacity stress tests; then integrate Queue. SignalR may announce committed status changes but never owns or executes reservations.
