# 50 — Portal Cashier and Admin Billing Oversight

Owner: Billing + Portal + Admin + Blazor UI + security  
Status: Verified role-facing reference slice  
Reviewed: 2026-08-23

## Outcome

The policy-neutral Billing foundation in [DOC-049](49-billing-invoice-payment-receipt-foundation.md) now has its first authorized user journey:

- Portal exposes a branch-scoped Cashier transaction for patient search, service selection, invoice issue, split-tender payment, allocation, receipt evidence and browser printing;
- Admin exposes read-only recent invoice and payment registers, filtered independently by Invoice/View and Payment/View permissions;
- shared Blazor UI supplies reusable financial status and money-summary components plus deterministic cashier draft state;
- API/Client add capped, permission-protected recent invoice and payment queries;
- no refund, GST, discount, close, export or destructive posted-finance operation was invented.

This is an implementation reference, not production finance acceptance.

## Host and permission boundary

### Portal Cashier

Route: `/billing/cashier`

The complete transaction card/route requires all of:

- `Patients.Search`;
- `Catalog.View`;
- `Billing.Invoices.View`;
- `Billing.Invoices.Issue`;
- `Billing.Payments.View`;
- `Billing.Payments.Receive`;
- `Billing.Payments.Allocate`.

This intentionally composes capabilities instead of checking a `Cashier` role name. The route repeats the permission gate; API authorization remains authoritative.

The journey:

1. uses the authenticated branch scope and masked Patient search;
2. loads only active Clinical Services;
3. captures quantity/unit price while the API owns totals and `BASIC-NET-V1` policy;
4. retains invoice request identity for safe Retry;
5. accepts Cash, Card, Cheque, Bank Transfer, UPI or Other split tenders;
6. retains payment request identity for safe Retry;
7. constrains allocation to both Invoice balance and Payment remainder and submits both versions;
8. reloads the authorized Invoice after allocation;
9. starts a fresh idempotent Payment identity—without reissuing the Invoice—when a fully allocated payment leaves the Invoice part-paid;
10. flags any payment remainder that is still unallocated rather than silently classifying it as an advance;
11. displays shortened immutable snapshot hashes and prints only the receipt through normal browser printing;
12. resets every request identity only for a genuinely new transaction.

No raw snapshot JSON, unmasked contact, internal numeric key or secret is rendered.

### Admin oversight

Route: `/billing/oversight`

The screen is visible with either `Billing.Invoices.View` or `Billing.Payments.View`. Each register is loaded only when its own permission is present. It shows protected Patient IDs, posted numbers, times, amounts, status, tender method names and shortened immutable hashes. It has no mutation controls.

Recent queries are newest-first and capped server-side to 100 rows. This is operational oversight, not reporting, reconciliation, export or an accounting ledger replacement.

## Shared UI and state

`BookDoc2026.Blazor.UI` now owns:

- `CashierBillingDraft` for line/tender composition, request-identity lifecycle and allocation constraints;
- `BillingStatusBadge` for issued/partial/settled states;
- `BillingMoneySummary` for reusable total/allocated/balance presentation.

The draft does not perform business persistence or authorization. Contracts/Client remain the only UI-to-API boundary, so the same state/components can support a later approved MAUI cashier view without moving Billing rules into a frontend.

## API additions

- `GET /api/v1/branches/{branchId}/billing/invoices?take=50` — `Billing.Invoices.View`;
- `GET /api/v1/branches/{branchId}/billing/payments?take=50` — `Billing.Payments.View`.

Both queries remain tenant/branch scoped, return protected identifiers and clamp `take` to 1–100. Existing issue, receive, allocate and item-read endpoints are unchanged.

No database migration was required. The queries use existing indexes and immutable aggregates. Broader pagination/projections must replace the bounded aggregate query before high-volume register/report use.

## Verification

Four new unit tests prove:

- invoice totals and retry identity lifecycle in the cashier draft;
- split tender preservation and post-confirmation immutability;
- allocation fit/version evidence, identity rotation after success and a second Payment against a part-paid Invoice;
- Admin oversight requires an Invoice or Payment read permission, not a mutation-only permission.

The Billing API integration journey now also proves:

- list endpoints reject mutation-only permission;
- authorized lists return the correct branch document numbers;
- oversized `take` is safely capped;
- the previously verified idempotency, immutable snapshots, concurrency, allocation and tenant isolation remain intact.

The complete suite is **122 tests**: 78 unit, 9 architecture and 35 integration tests.

## Deliberately deferred

- finance-owner acceptance and real browser cashier UAT;
- price-list/tariff selection and cashier price-override permissions;
- GST, discounts, approval thresholds and rounding;
- refund, reversal, credit note, void and advance credit;
- cash drawer/session, daily close, discrepancy and reconciliation;
- payment gateway/callback processing;
- PDF generation/storage/delivery and local print-agent operation;
- Admin export/report schedules and high-volume projection/pagination;
- MAUI cashier/receipt journey and offline policy.

## Achievement effect and next recommendation

DOC-033 row 6 rises from 65% to 70% because Admin now has another permission-filtered ERP oversight surface. Row 7 rises from 70% to 75% because Portal now has its first finance journey. Row 15 rises from 40% to 50% because invoice/payment/allocation is now usable through authorized host journeys, while policy, reconciliation and migration parity remain open. The exact twenty-row score becomes **73.25%**.

The recommended next slice is **specialty clinical delivery for Physiotherapy**: care plan, treatment-session progression and outcome measures connected to the existing Encounter/Practitioner/Booking foundations. It should remain policy-neutral until the specialty workshop approves templates and state transitions. GST/refund/daily-close work remains blocked on finance-owner decisions.
