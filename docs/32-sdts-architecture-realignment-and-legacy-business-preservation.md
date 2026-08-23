# 32 — SDTS Architecture Realignment and Legacy Business Preservation

Owner: Architecture + identity + Scheduling + Billing  
Status: Architecture foundation implemented; business-module specification approved for implementation  
Last reviewed: 2026-08-17

## Purpose

This document prevents two costly mistakes:

1. rebuilding BOOKDOC2026 with the structural patterns of the old BookDoc application; or
2. adopting the SDTS project structure while losing the proven booking, contract and payment behavior.

The source authority is deliberately split:

| Concern | Authoritative source | Treatment in BOOKDOC2026 |
|---|---|---|
| Solution composition, hosts, dependency direction, Identity, JWT, users, roles, permissions and client boundaries | `SDTS.ERP2026` | Adopt and improve for multi-tenant healthcare |
| Booking, package/contract, tariff, visit consumption, payment, allocation and adjustment behavior | `BookDocAppointment` plus live-user validation | Preserve as requirements and rebuild as modern domain rules |
| Tenancy and scope | New BOOKDOC2026 decisions | Tenant → Organization → Branch with durable user scopes |
| What can be reserved | New BOOKDOC2026 generalized model | Practitioner, room/space, bed/chair, imaging modality, equipment, team and service point |
| Legacy controllers, EF6 repositories, stored-procedure calls, views and database shape | Neither target nor authority | Do not port |

## Implemented architecture baseline

The solution now contains the SDTS-style host and shared-project boundaries:

```text
Three product applications
├── BookDoc2026.Api (backend plus hosted immediate/scheduled jobs)
├── BookDoc2026.Admin (ERP-style restricted frontend)
└── BookDoc2026.Portal (authenticated internet-facing frontend)

Development orchestration
└── BookDoc2026.AppHost (starts API, Admin and Portal)

BookDoc2026.Bootstrap (guarded one-time operator provisioning command)

Shared composition and boundaries
├── BookDoc2026.ServiceDefaults
├── BookDoc2026.Worker (background-job class library hosted by API)
├── BookDoc2026.Client
├── BookDoc2026.Blazor.UI
├── BookDoc2026.Contracts
└── BookDoc2026.Shared.Kernel

Backend layers
BookDoc2026.Domain <- BookDoc2026.Application <- BookDoc2026.Infrastructure
```

- Admin and Portal call the API through `BookDoc2026.Client`; they do not reference Domain, Application or Infrastructure. The development AppHost also exposes MAUI Windows as an explicit-start resource that receives API's current HTTPS endpoint; Android and iOS remain device/emulator launches.
- API is the only backend host. Worker is a class library registered by API; it shares Application and Infrastructure handlers and does not call or reference API.
- API also composes three non-host libraries: `BookDoc2026.Messaging`, `BookDoc2026.Templates` and `BookDoc2026.DocumentService`. Messaging depends on Templates; generated-document export remains separate from storage; none references API or becomes another runtime process.
- `BookDoc2026.ErrorHandling` is a fourth platform-neutral library shared by API, Client and Worker. HTTP status mapping stays in API, presentation stays in each UI host, and domain exception ownership stays in Domain.
- `BookDoc2026.Shared.Kernel` owns the small security vocabulary shared across trusted layers.
- `BookDoc2026.Blazor.UI` contains reusable Razor presentation primitives only.
- MAUI consumes the shared Client/API boundary, reuses presentation primitives from `BookDoc2026.Blazor.UI`, and uses `BookDoc2026.Maui.UI` for native services and controls. The current scaffold includes secure token storage but no approved clinical journey yet.

## Identity and authorization decision

The temporary branch-user/header-auth design is superseded by the SDTS-derived Identity model:

- ASP.NET Core Identity users and roles use `uint` CLR identifiers.
- Seeded foundation roles are `PlatformOperator` and `ClinicAdministrator`.
- Role claims carry named permissions.
- `ApplicationUserScope` durably grants a user a Tenant, Organization and optional Branch scope; a database foreign key proves that a branch belongs to the same tenant and organization.
- JWT access tokens carry subject, roles, permissions and the selected durable scope.
- Refresh tokens are random, stored only as hashes, bound to the selected scope, rotated on refresh and invalidated on replay.
- A platform operator follows a separate control-plane path. A tenant user must resolve an active assigned scope.
- Development headers remain available only in Development/Testing. Bearer JWT is the runtime authentication path outside tests.
- No signing key, bootstrap password or provider secret is committed. `BookDoc2026.Bootstrap` creates only the first platform operator, requires an explicit confirmation plus secret/environment credentials, refuses pending migrations and refuses a second operator.

Authorization always combines host access, permission and durable scope. Encrypted public identifiers make IDs opaque; they do not replace authorization.

## Identifier interpretation

The SDTS unsigned-key style is adopted selectively where it produces a safe contract:

| Area | CLR type | SQL Server storage | Public contract |
|---|---|---|---|
| Identity user and role | `uint` | EF maps to `bigint` because SQL Server has no unsigned integer type | protected `string` when referenced publicly |
| Tenant and clinical/business aggregates | positive `long` | `bigint` | tenant/type-bound protected `string` |
| High-volume transactional aggregates | positive `long` initially | `bigint` | protected `string` |
| Idempotency request | `Guid` | `uniqueidentifier` | `Guid` request field |
| Bounded enums/reference codes | `byte`, `ushort`, `int` only when capacity and compatibility are proven | compatible signed SQL type | code or protected ID as appropriate |

`ulong` is not used for SQL-owned aggregate keys because SQL Server has no native unsigned 64-bit type. This preserves the user's intention—compact typed internals and encrypted string DTO identifiers—without introducing value-conversion, sorting and interoperability hazards. See [Numeric and Protected Identifier Reference](31-numeric-and-protected-identifier-reference.md).

## Legacy behavior inventory to preserve

### Booking evidence

Legacy `BookingInfo` proves these requirements:

- unique human booking number;
- booking date plus from/to time;
- a Contract link;
- one primary `Object` (historically presented as Doctor);
- location/accommodation;
- procedure lines and associate objects;
- notes, send-address preference, walk-in and family-member context;
- follow-up indicator;
- creation/update actors and timestamps;
- appendable status history including actual status time, entry time, user and description.

Legacy status vocabulary includes Booking, CheckIn, Consulting, CheckOut, Waiting, Cancel, Reschedule, Event and Activity. These values are evidence, not the new state machine. The modern workflow separates booking lifecycle, queue lifecycle and encounter lifecycle so that “Waiting” and “Consulting” are not overloaded booking states.

### Contract/package evidence

Legacy `ContractInfo`, `ContractTariffInfo`, contract extension and `ObjectContractInfo` prove these requirements:

- package/contract number, date, type, customer/patient and location;
- validity from/to dates;
- sold visits, consumed visits and visits remaining;
- package and per-visit pricing;
- option/booked/invoiced/cancelled progression;
- expiry and refund effects;
- category/location-effective tariffs;
- extensions and notes;
- practitioner/object compensation or value calculation by booking or contract basis.

The new model must use decimal money and immutable price snapshots. It must not reproduce the legacy integer division in the computed visit tariff or rely on database-computed business state.

### Payment evidence

Legacy `PaymentInfo` and pay-mode records prove these requirements:

- unique payment/receipt number and payment date;
- patient, contract, booking and invoice associations;
- receiver, branch/location, category, coupon and on-account company context;
- split tender across cash, card, cheque, other mode and due/PDC concepts;
- advance, due clearance, refund and adjustment behavior;
- allocation/arrangement between due and clearing entries;
- procedure attribution, notes and audit actor/time;
- balance and received/due/refund totals.

The new design represents these with Payment, PaymentTender, PaymentAllocation, Advance/Credit, Refund/Reversal and Adjustment records. Posted financial rows are immutable; corrections append compensating records. Provider callbacks and commands require idempotency.

## Generalized contract and booking model

“Bookable object” is not a Doctor entity. It is a categorized resource with capabilities and availability.

```text
Patient/Stakeholder
      │
      ├── Contract or Package
      │     ├── ContractLine / Entitlement
      │     ├── PriceSnapshot
      │     ├── Validity / VisitBalance
      │     └── ContractEvent
      │
      └── Booking
            ├── BookingService / Procedure
            ├── BookingResource (one or many)
            ├── BookingEvent / StatusHistory
            ├── FollowUp / RescheduleLineage
            └── Invoice / PaymentAllocation
```

`BookingResource` contains a resource and a role such as Primary, Required, Assist or Supervise. Examples:

- consultation: primary practitioner + required room;
- physiotherapy session: practitioner/team + treatment space + equipment;
- X-ray/CT: modality + technician/team + room;
- day-care: bed/chair + supervising team;
- future inpatient admission: reservation may use Scheduling, but actual occupation belongs to an Inpatient `BedAllocation` with transfer/discharge history.

A resource category controls relevant behavior; one nullable DoctorId/BedId/MachineId set on Booking is prohibited.

## Target aggregates and boundaries

| Module | Aggregate candidates | Important rule |
|---|---|---|
| Catalog/Resources | Service, Procedure, ResourceCategory, BookableResource, ResourceCapability | Resource kind is data-driven but behavior is validated by typed capability rules |
| Contracts | Contract, ContractLine, Entitlement, ContractPriceSnapshot, ContractEvent | Contract consumption is transactional and cannot become negative |
| Scheduling | AvailabilityRule, Hold, Booking, BookingResource, BookingEvent | All required capacity is acquired atomically; overlap is rejected |
| Queue | QueueTicket, QueueStageEvent, ServicePoint | Queue state does not silently mutate Booking state |
| Billing | Invoice, Payment, Tender, Allocation, Refund/Reversal, Adjustment, CashSession | Posted money is immutable and fully reconcilable |
| Inpatient (future) | Admission, BedAllocation, Transfer, Discharge | A bed reservation is not proof of occupancy |

The Contract module may initially live under Scheduling or Billing as a bounded feature folder, but its ownership and tables must remain explicit. It must not become a generic shared table.

## Rules that must be proven before implementation acceptance

### Contract and entitlement

1. Define whether cancellation, no-show and reschedule consume a visit for each contract type.
2. Define expiry, extension, freeze and refund rules.
3. Reserve/consume/release entitlement in the same transactional workflow as booking confirmation/cancellation.
4. Store the applied tariff and rule version as a snapshot.
5. Reject negative remaining visits and duplicate consumption.

### Booking and resources

1. A hold is not a booking; confirmation converts a valid hold atomically.
2. Every required resource and capacity unit must still be available at confirmation.
3. Resource status, branch, capability and service compatibility are validated server-side.
4. Rescheduling retains lineage and does not overwrite the prior reservation.
5. Status changes append actor, reason and actual/effective time.
6. Bed category requests may be satisfied by a bed only through an explicit allocation policy; inpatient occupancy is separate.

### Billing and payment

1. Invoice issue snapshots description, quantity, decimal price, discount, tax and total.
2. Split tender totals and allocations reconcile exactly to payment totals.
3. Advance, due, refund, reversal and adjustment have distinct typed commands and permissions.
4. Receipt/invoice numbers are unique within the approved tenant/branch/financial scope.
5. Retry or provider callback replay cannot post money twice.

## Implementation sequence

1. Accept the Identity/JWT and durable-scope foundation, including the bootstrap/deployment procedure.
2. Approve a Contract/Package rule matrix from live users and anonymized production evidence.
3. Implement Contract and entitlement primitives with decimal pricing and concurrency tests.
4. Extend the confirmed multi-resource Booking reference in DOC-040 with status history, cancellation, waitlist and reschedule lineage.
5. Integrate Queue and Encounter transitions through commands/events, not a shared status field.
6. Implement invoice issue, Payment/Tender/Allocation, then refund/reversal/adjustment.
7. Migrate legacy records through crosswalks and disposition rules; never run legacy repository or stored-procedure code in the target.

## Acceptance evidence

- Architecture tests keep Admin, Portal, Client and shared UI out of backend layers.
- JWT tests cover platform permissions, refresh rotation/replay rejection and tenant default-scope authorization.
- SQL migration creates Identity, role/claim and durable user-scope tables and removes the temporary branch-user/grant tables.
- EF reports no pending model changes after migration.
- Contract, booking and payment implementation cannot start until their state/rule matrices and reconciliation examples have named owner approval.

## Explicitly deferred

- Employee and payroll ownership/integration until the promised database is supplied.
- Hospital inpatient occupancy, transfer and discharge implementation.
- A complete MAUI host and offline behavior.
- Legacy data migration execution until production schema/profile evidence is available.
