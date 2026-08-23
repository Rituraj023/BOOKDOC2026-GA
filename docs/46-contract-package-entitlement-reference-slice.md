# 46 — Contract and Package Entitlement Reference Slice

Owner: Contract + Scheduling + backend + security + migration  
Status: Verified policy-neutral reference slice  
Reviewed: 2026-08-23

## Outcome

BOOKDOC2026 now has a tenant- and branch-scoped Contract/Package entitlement ledger that can reserve and consume visits for a confirmed generalized Booking. Eligibility can target a clinical service and, optionally, a resource category such as a practitioner, room, bed/chair, imaging modality, equipment or team. The implementation does not assume that the bookable resource is a doctor.

This slice deliberately stops before automatic Booking lifecycle coupling. [DOC-049](49-billing-invoice-payment-receipt-foundation.md) subsequently adds policy-neutral invoices, payments and allocations, but refunds, automatic entitlement coupling and daily close still require an owner-approved rule matrix; the implementation does not silently inherit legacy defects or invent financial policy.

## Legacy evidence preserved as requirements

The legacy `ContractInfo`, `ContractTariffInfo`, `ContractExtenedInfo` and `ObjectContractInfo` models and `SP_INCLUDEBOOKINGINPACAGE` procedure establish useful vocabulary:

- a patient contract has a number, type/category, validity dates, state, sold visits, visited/remaining visits and package tariff;
- contracts may be extended and Booking may consume a package visit;
- resource/provider compensation and package pricing are distinct concerns.

The legacy entities, EF6 mapping, `double` money, computed `VisitLeft`, integer-cast visit-price calculation and stored-procedure mutation were not ported. The target uses decimal money, explicit ledger counters, optimistic versions, protected public IDs, tenant-qualified relationships and auditable commands.

## Implemented model

`ContractAgreement` owns branch, patient, contract number/type, inclusive validity, active status, package price/currency, immutable rule-version reference, notes and concurrency version.

`ContractEntitlement` owns service, optional resource category, total/reserved/consumed units, derived available units, unit price/currency, rule version and concurrency version. Its invariant is:

```text
available = total - reserved - consumed
reserved >= 0
consumed >= 0
reserved + consumed <= total
```

Availability is derived rather than separately persisted. SQL check constraints enforce the ledger bounds. Separate filtered unique indexes prevent duplicate service-only entitlements and duplicate service-plus-category entitlements.

`EntitlementReservation` links one entitlement to one confirmed Booking. It has a caller-generated request ID and deterministic request hash for replay safety, a unit count, `Reserved -> Consumed | Released` lifecycle, timestamps, release reason and concurrency version.

Reserve verifies permission and durable branch scope, protected identities, Contract/Entitlement/Booking ownership, confirmed Booking status, matching patient and service, matching allocated resource category when required, inclusive validity in the branch time zone, sufficient units/version and identical replay content.

## Public API and permissions

All identifiers exposed by the API are tenant-bound protected strings.

| Operation | Endpoint | Permission |
|---|---|---|
| Create Contract plus Entitlements | `POST /api/v1/branches/{branchId}/contracts` | `Contracts.Manage` |
| Read Contract ledger | `GET /api/v1/branches/{branchId}/contracts/{contractId}` | `Contracts.View` |
| Reserve Booking units | `POST /api/v1/branches/{branchId}/contracts/{contractId}/entitlements/{entitlementId}/reservations` | `Contracts.Entitlements.Reserve` |
| Consume reserved units | `POST /api/v1/branches/{branchId}/contracts/entitlement-reservations/{reservationId}/consume` | `Contracts.Entitlements.Consume` |
| Release reserved units | `POST /api/v1/branches/{branchId}/contracts/entitlement-reservations/{reservationId}/release` | `Contracts.Entitlements.Release` |

The typed Client exposes the same operations. The permissions are branch-assignable and were appended to seeded role claims without renumbering previously shipped claims.

## Persistence and migration

Migration `20260823110228_ContractEntitlementFoundation` adds schema `contract` with `contract`, `entitlement` and `entitlement_reservation` tables. Relationships are tenant-qualified and deletion is restricted. Unique indexes protect contract numbers, entitlement definitions, Booking linkage and request replay. Concurrency tokens protect ledger mutations. Contract creation and reservation write their audit event in the same transaction as business state.

The local development database was upgraded successfully and EF reports no pending model changes. During verification, the previously untracked queue migration source was safely regenerated as `20260823110200_ImagingQueueAndRealtimeRestored` and reapplied before Contract; the local queue schema and permission data were restored with no application-data loss.

## Automated evidence

Four domain tests prove reserve/consume arithmetic, release restoration and reason validation, insufficient-unit/stale-version rejection and inclusive validity.

The API integration test provisions a real tenant/branch, Patient, Physiotherapy service, room-category resource and confirmed Booking, then proves:

- manage/view/reserve/consume permission separation and forbidden responses;
- protected public Contract identifiers and resource-category-aware eligibility;
- first request versus identical replay, and changed-content replay rejection;
- persisted reserve/consume counters and stale-transition conflict.

## Explicitly deferred owner decisions

Before Booking automatically reserves, consumes or releases units, named Product/Contract/Billing owners must approve:

1. reservation and consumption points;
2. cancellation, reschedule, no-show and late-cancellation reversal rules;
3. expiry handling for already reserved visits;
4. extension, transfer, freeze/suspension and partial-unit rules;
5. overuse/manual adjustment authority and reasons;
6. price, discount, tax and invoice snapshots;
7. refund, payment allocation, adjustment and daily-close reconciliation;
8. multi-entitlement or multi-line consumption;
9. migration treatment for legacy sold, visited and inconsistent remaining totals.

Until approved, reserve/consume/release remain explicit authorized commands. Booking cancellation does not automatically release entitlement and consumption does not create financial entries.

## Remaining acceptance work

- validate the complete rule matrix with users and representative anonymized data;
- add status management, extensions and approved adjustment history;
- couple Booking lifecycle only after reversal rules are accepted;
- add finance-owned Billing/refund/close slices;
- add concurrent SQL, cross-tenant and representative migration reconciliation;
- add authorized Admin/Portal Contract and package-balance journeys.

This earns DOC-033 row 15 an increase from 15% to 30%, not 50% or 75%: the durable direction works end to end, but owner validation and the financial lifecycle remain substantial open work.
