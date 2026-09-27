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
- `FR-INV-001`: an Investigation Order requires a signed Encounter and an active Catalog Service configured through a non-optional X-ray/CT imaging-modality resource category; the API rejects an unconfigured service or modality mismatch. DOC-055 implements this requirement.
- `FR-INV-005`: Queue, Order, Study/acquisition, technical quality, report version, critical-result communication/acknowledgement, clinical release, patient release and document snapshot remain independently authorized and audited; no one state is inferred from another. DOC-058 implements and tests this separation through technical quality only; later states remain absent and gated by DOC-057.
- `FR-RAD-001`: one active queued X-ray/CT Investigation Order may register one tenant-scoped Study; registration/start require branch permission plus current Practitioner credential and service assignment. DOC-058 implements this requirement.
- `FR-RAD-002`: acquisition requires the expected Study version, an idempotency request identity, a matching active/available modality resource with ordered-service capability, protocol/version, structured deviation/outcome evidence and valid timing. The immutable attempt and append-only event must survive replay without duplication. DOC-058 implements this requirement.
- `FR-RAD-003`: a separately permissioned and eligible actor may review only the latest acquired attempt, cannot self-review, and may explicitly accept quality or require repeat without overwriting prior evidence. DOC-058 implements this requirement.
- `FR-RAD-004`: contracts expose protected IDs and only the presence of an external PACS reference; raw external references and notes do not enter general audit/SignalR payloads. DOC-058 implements the API/audit portion; PACS integration remains deferred.
- `FR-RAD-005`: Portal exposes Study/acquisition commands only to an eligible operator capability and technical-quality commands only to a separately permissioned reviewer capability; it lists only eligible equipment, preserves stable request identity across retry, renders immutable attempt/review correlation and keeps Queue/Study/Result state visibly separate. [DOC-059](59-portal-radiology-execution-quality-workspace.md) implements and proves this requirement with synthetic authenticated roles.
- `FR-INV-002`: Order, Result and Queue states have separate owners; creating/completing a Queue ticket cannot mark the Order performed or the Result complete. DOC-055 implements the first `Requested`/`Pending`/Queue separation.
- `FR-INV-003`: order request and Queue handoff are separately permissioned, tenant/branch scoped, idempotent, concurrency checked and audited; one Order can create at most one matching-modality Queue ticket. DOC-055 implements this requirement.
- `FR-INV-004`: a technician ordered-work query requires `Investigations.Worklist.View`, is bounded and branch/service-point scoped, returns only active Queue-linked Orders plus minimum Patient/Encounter/Service/indication references, and does not imply generic Queue visibility, transition authority or access to Encounter narrative/results/documents. DOC-056 implements this requirement.
- `FR-RX-001`: issued prescriptions retain the exact issued representation and practitioner identity.
- `FR-BIL-001`: issued invoices preserve numbering, price and tax snapshots and support reasoned reversal, not mutation.
- `FR-BIL-002`: confirmed payments preserve split tender and immutable receipt evidence; append-only allocation cannot exceed either the Payment remainder or same-patient/currency Invoice balance. DOC-049 implements the non-refund foundation; tax/reversal acceptance remains open.
- `FR-BIL-003`: an authorized branch Cashier can search a masked Patient, compose service lines, issue an idempotent Invoice, receive split tender, allocate safely and browser-print receipt evidence; Admin separately exposes only the invoice/payment registers allowed by its read permissions. DOC-050 implements the reference journey; finance UAT and final policy remain open.
- `FR-COM-001`: communication checks template version, consent/preference, channel policy and idempotency before delivery.
- `FR-REP-001`: report access requires allowed host, report permission and tenant/branch/data scope.
- `FR-REP-002`: only Admin-authorized users schedule reports; execution re-evaluates scope and recipient eligibility.
- `FR-PRN-001`: normal documents support browser/PDF; the agent accepts only authorized receipts, queue slips and labels.
- `FR-MOB-001`: mobile tokens and cache are protected; push/SignalR payloads contain no sensitive clinical content.
- `FR-PHY-001`: physiotherapy assessment, goals, outcome measures, care-plan versions and session notes preserve chronological clinical history. DOC-051 implements the policy-neutral care-plan/session/outcome subset; approved assessment fields remain gated.
- `FR-PHY-002`: therapy packages and session consumption cannot change or erase clinical session documentation.
- `FR-PHY-003`: a Physiotherapy care plan starts from a signed Physiotherapy Encounter, and every mutation requires an active verified Practitioner assigned to its branch and service.
- `FR-PHY-004`: Active care-plan changes append a reasoned revision; treatment sessions and outcome observations are immutable, tenant/branch scoped and never overwritten by financial/package activity.
- `FR-PHY-005`: the Portal Physiotherapist worklist is permission- and branch-scoped, bounded, excludes clinical narrative, and loads full history only for a selected authorized care plan. DOC-052 implements this reference behavior.
- `FR-PHY-006`: outcome trends compare only matching measure code, tool version, unit, body site and laterality; the product does not infer clinical improvement or ship an unapproved scale catalog.
- `FR-CLN-005`: the practitioner agenda is a bounded local-day query that returns only confirmed Bookings matching the actor's active credential and effective branch/Service/resource assignment; it excludes clinical narrative. DOC-053 implements this reference behavior.
- `FR-CLN-006`: Portal signing applies only to the persisted latest Encounter revision; unsaved form changes must be saved as a new draft revision before signing.
- `FR-CLN-007`: a specialty handoff is offered only for an explicitly matching signed Encounter and independently authorized destination module; public protected tokens are never compared as stable identities.
- `FR-ORT-001`: orthopaedic assessment/procedure records require explicit body site and laterality when clinically applicable.
- `FR-ORT-002`: imaging/results, prescriptions, procedures and external referrals remain linked to the encounter without implying unperformed care.

## Acceptance evidence

Each requirement needs at least one happy path, two relevant exception paths, authorization-negative tests, tenant-isolation tests, audit evidence and product-owner acceptance. Clinical and financial requirements additionally need concurrency and immutable-history evidence. Migrated behavior requires source-to-target reconciliation or an approved intentional difference.

Priority uses `Must`, `Should`, `Could`, `Deferred`. A `Must` requirement without an owner, acceptance examples or resolved safety/security dependency cannot enter implementation.
