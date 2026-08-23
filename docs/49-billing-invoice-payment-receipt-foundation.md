# 49 — Billing Invoice, Payment and Receipt Foundation

Owner: Billing + Patient + Scheduling + Contracts + security + migration  
Status: Verified policy-neutral backend reference slice  
Reviewed: 2026-08-23

## Outcome

BOOKDOC2026 now has the first posted-finance foundation: idempotent issued invoices, immutable service/price lines, confirmed split-tender payments, append-only payment allocations, branch-configured invoice/receipt numbers, and immutable canonical invoice/receipt snapshots with SHA-256 evidence.

This slice intentionally does not invent Delhi/India GST, discount approval, refund, reversal, advance-credit, cash-session or cancellation behavior. Until named financial owners approve those matrices, invoices use calculation policy `BASIC-NET-V1`: quantity × unit price, with zero discount and tax. The model retains explicit discount/tax totals so a later approved policy can be additive, but callers cannot submit totals.

## Legacy evidence and redesign

Legacy `InvoiceInfo`/`InvoiceDetailInfo`, `PaymentInfo`, `PaymentPaymodeInfo`, `PaymentPaymodeArrangeInfo` and `PaymentPaymodeAdjustInfo` prove requirements for unique document numbers, patient/Booking/Contract relationships, quantity and tariff lines, split cash/card/cheque/other tenders, due/clearing arrangements, balances, receipt reporting and actor/time evidence.

The target does not port editable/delete EF6 services, local timestamps, globally overloaded Doctor/Object fields, database-computed balances, or the ambiguous `Due | ClearDue` plus `PDC | Advance | Refund | Adjust` flag combinations. Posted financial details are typed and append-only. Refund, reversal, adjustment and advance will receive separate aggregates/commands only after policy approval.

## Implemented aggregates and rules

### Invoice

- references one tenant, branch and Patient;
- may reference a confirmed same-patient Booking and/or same-patient Contract without automatic lifecycle coupling;
- generates an immutable number from the current branch invoice prefix and numeric entity identity;
- snapshots service code/name, quantity, decimal unit price and server-calculated line total;
- derives subtotal and total on the server; callers cannot submit computed totals;
- moves `Issued -> PartPaid -> Paid` only through allocation;
- uses optimistic versioning and cannot be deleted.

### Payment and allocation

- Payment posting is idempotent by tenant request ID plus canonical request hash;
- a Payment contains one or more immutable `Cash | Card | Cheque | BankTransfer | Upi | Other` tenders;
- the receipt amount is the exact decimal sum of tenders;
- unallocated money may remain on the Patient payment record without being mislabeled as refund or advance policy;
- allocation is a separate idempotent append-only record;
- Payment and Invoice patient, branch, tenant and currency must match;
- one transaction/version check updates both allocated balances and appends the allocation;
- allocation cannot exceed either payment remainder or invoice balance.

### Immutable document evidence

Invoice issue and payment receipt capture `FINANCIAL-DOCUMENT-V1` canonical JSON plus SHA-256 hash in `billing.financial_document_snapshot`. The snapshot is branch/tenant constrained, unique by kind/source, and cannot be updated/deleted through `BookDocDbContext`.

This is the immutable financial truth input for later HTML/PDF/DOCX rendering. It is not yet a stored rendered PDF and does not claim final legal layout, GST fields, signature or patient-delivery acceptance.

## Permissioned API

| Operation | Endpoint | Permission |
|---|---|---|
| Issue/replay Invoice | `POST /api/v1/branches/{branchId}/billing/invoices` | `Billing.Invoices.Issue` |
| Read Invoice | `GET /api/v1/branches/{branchId}/billing/invoices/{invoiceId}` | `Billing.Invoices.View` |
| Receive/replay Payment | `POST /api/v1/branches/{branchId}/billing/payments` | `Billing.Payments.Receive` |
| Read Payment/receipt/allocation evidence | `GET /api/v1/branches/{branchId}/billing/payments/{paymentId}` | `Billing.Payments.View` |
| Allocate Payment to Invoice | `POST /api/v1/branches/{branchId}/billing/payments/{paymentId}/allocations` | `Billing.Payments.Allocate` |

All five permissions are branch-assignable and enforced at controller and application boundaries. Invoice, line, Payment, tender, allocation and snapshot identifiers are tenant/type-bound protected strings. The typed Client exposes all operations.

Audits retain document number, internal relationship IDs, currency, amount/count, versions and snapshot hash. Patient names/contact data, notes, tender references and snapshot payloads are excluded from audit metadata.

## Persistence

Migration `20260823114521_BillingInvoicePaymentFoundation` creates:

- `billing.invoice`;
- `billing.invoice_line`;
- `billing.payment`;
- `billing.payment_tender`;
- `billing.payment_allocation`;
- `billing.financial_document_snapshot`;
- ten append-only seeded role claims for five platform and five clinic permissions.

SQL checks enforce positive posted totals, precision-safe allocation limits, tender methods and states. Composite foreign keys prove tenant/branch ownership for allocations and snapshots. Unique indexes protect request IDs and branch document numbers. The migration is applied to local `BookDoc2026_Dev`; EF reports no pending model changes.

## Automated evidence

Five domain tests prove server totals, split-tender sums, atomic balance progression, failure without partial in-memory mutation, mismatch/over-allocation rejection and deterministic snapshot hashing.

One full API integration journey proves:

- negative Invoice and allocation authorization;
- idempotent Invoice and Payment replay plus changed-payload rejection;
- protected IDs and server-calculated 2 × ₹125 invoice total;
- ₹100 cash + ₹200 UPI split receipt;
- partial then full invoice allocation while ₹50 remains unallocated;
- stale-version conflict;
- unchanged Invoice/receipt hashes after later allocations;
- `BookDocDbContext` snapshot-delete rejection;
- cross-tenant protected-ID rejection.

The complete suite is **118 tests**: 74 unit, 9 architecture and 35 integration tests.

## Deliberately deferred

- GST/CGST/SGST/IGST, SAC/HSN, place-of-supply, exemption and rounding policy;
- price-list/tariff selection, discounts, coupons and approval thresholds;
- invoice draft/estimate, void/credit note and reasoned correction;
- refund, provider reversal, chargeback, adjustment and advance-credit classification;
- Contract entitlement automatic reserve/consume/release and Booking cancellation/no-show effects;
- cash drawer/session, day close, discrepancy and reconciliation;
- payment gateway orders, verified callback inbox and provider idempotency;
- PDF/DOCX rendering, object storage, patient delivery and print-agent integration;
- MAUI receipt view and production finance/report acceptance; Portal Cashier and read-only Admin oversight are implemented in [DOC-050](50-portal-cashier-and-admin-billing-oversight.md);
- live legacy mapping, financial control totals, performance and named finance UAT.

## Achievement effect and next recommendation

DOC-033 row 15 rises from 30% to 40% because modern Invoice, Payment, tender and allocation behavior now exists beside the Contract ledger. Row 18 rises from 25% to 30% because issued invoices/receipts now retain immutable canonical snapshot evidence, though rendered-document storage and reporting/printing remain open. The exact twenty-row architecture score becomes **72.25%**.

The recommended role-facing slice is now implemented in [DOC-050](50-portal-cashier-and-admin-billing-oversight.md). Refund/reversal, GST, price-policy and daily-close controls remain blocked until owner approval.
