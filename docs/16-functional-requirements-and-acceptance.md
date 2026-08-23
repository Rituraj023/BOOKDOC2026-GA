# 16 — Functional Requirements and Acceptance

## Requirement format

```text
Requirement ID and title:
Actor and measurable outcome:
Preconditions and triggering event:
Normal and exception behavior:
Permission, tenant, branch and record scope:
State/invariant/idempotency:
Data, report, print and notification effects:
Audit/privacy/retention:
Acceptance examples and automated test IDs:
Owner, priority, release and decision dependencies:
```

## Baseline requirements

- `FR-TEN-001`: every tenant-owned operation rejects missing, forged or unauthorized tenant scope.
- `FR-TEN-002`: platform support access is separately granted, time-bound, reasoned and audited.
- `FR-TEN-003`: only an authorized platform operator can approve and activate a clinic tenant after required verification evidence is recorded.
- `FR-TEN-004`: tenant approval, rejection, suspension, reactivation and closure preserve actor, reason, evidence and immutable history.
- `FR-TEN-005`: onboarding uses versioned configurable document types linked to authorized secure files; required, missing, expired, rejected and replaced evidence is explicit.
- `FR-TEN-006`: tenant branding, numbering and communication identities are inherited by branches unless an authorized branch override exists; issued artifacts retain the effective snapshot.
- `FR-TEN-007`: clinic-submitted and operator-created applications use the same review/approval gate and cannot produce active tenant access before approval.
- `FR-TEN-008`: only platform operators manage onboarding document-type definitions; authorized branch administrators self-manage allow-listed branch overrides with durable branch scope, version/concurrency checks and complete audit history.
- `FR-TEN-009`: a domain or communication identity that requires provider/legal verification cannot be used until verification succeeds, even though no BOOKDOC platform approval is required.
- `FR-IAM-001`: every command, query, dashboard card and report uses named permission plus durable scope.
- `FR-PAT-001`: patient search returns masked, tenant-scoped results and cannot auto-merge solely by name/mobile.
- `FR-PAT-002`: registration is idempotent; reusing a request ID with different data is rejected, and possible duplicates require a reasoned audited override rather than automatic merge.
- `FR-PAT-003`: demographic corrections require patient-update permission, durable branch scope and the current patient version.
- `FR-CAT-001`: a service declares required or optional resource-category roles without creating a reservation.
- `FR-CAT-002`: resources support practitioner, space, bed/chair, imaging modality, equipment, team and service-point categories with exclusive or pooled capacity.
- `FR-CAT-003`: unsafe category retirement, capability assignment and capacity reduction are rejected and audited mutations use optimistic versions.
- `FR-CAT-004`: resource availability, maintenance, cleaning and blocked changes require permission, reason and append-only branch-bound history.
- `FR-SCH-001`: simultaneous booking of the final provider/resource capacity allows exactly one accepted reservation.
- `FR-SCH-002`: provider, room, modality and other required resources reserve atomically or not at all.
- `FR-QUE-001`: queue actions are concurrency-safe; SignalR clients recover authoritative state through the API.
- `FR-ENC-001`: signed clinical records cannot be overwritten or deleted; corrections create traceable amendments.
- `FR-RX-001`: issued prescriptions retain the exact issued representation and practitioner identity.
- `FR-BIL-001`: issued invoices preserve numbering, price and tax snapshots and support reasoned reversal, not mutation.
- `FR-BIL-002`: confirmed payments preserve split tender and immutable receipt evidence; append-only allocation cannot exceed either the Payment remainder or same-patient/currency Invoice balance. DOC-049 implements the non-refund foundation; tax/reversal acceptance remains open.
- `FR-BIL-003`: an authorized branch Cashier can search a masked Patient, compose service lines, issue an idempotent Invoice, receive split tender, allocate safely and browser-print receipt evidence; Admin separately exposes only the invoice/payment registers allowed by its read permissions. DOC-050 implements the reference journey; finance UAT and final policy remain open.
- `FR-COM-001`: communication checks template version, consent/preference, channel policy and idempotency before delivery.
- `FR-REP-001`: report access requires allowed host, report permission and tenant/branch/data scope.
- `FR-REP-002`: only Admin-authorized users schedule reports; execution re-evaluates scope and recipient eligibility.
- `FR-PRN-001`: normal documents support browser/PDF; the agent accepts only authorized receipts, queue slips and labels.
- `FR-MOB-001`: mobile tokens and cache are protected; push/SignalR payloads contain no sensitive clinical content.
- `FR-PHY-001`: physiotherapy assessment, goals, outcome measures, care-plan versions and session notes preserve chronological clinical history.
- `FR-PHY-002`: therapy packages and session consumption cannot change or erase clinical session documentation.
- `FR-ORT-001`: orthopaedic assessment/procedure records require explicit body site and laterality when clinically applicable.
- `FR-ORT-002`: imaging/results, prescriptions, procedures and external referrals remain linked to the encounter without implying unperformed care.

## Acceptance evidence

Each requirement needs at least one happy path, two relevant exception paths, authorization-negative tests, tenant-isolation tests, audit evidence and product-owner acceptance. Clinical and financial requirements additionally need concurrency and immutable-history evidence. Migrated behavior requires source-to-target reconciliation or an approved intentional difference.

Priority uses `Must`, `Should`, `Could`, `Deferred`. A `Must` requirement without an owner, acceptance examples or resolved safety/security dependency cannot enter implementation.
