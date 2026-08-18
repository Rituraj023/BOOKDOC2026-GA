# 14 — Workflow and Use-Case Catalog

## Traceability convention

Every workflow receives a stable ID, actors, trigger, preconditions, normal path, exceptions, permission/scope, state transition, data/output, notification, audit event and acceptance owner.

## Foundation and tenant onboarding

- `TEN-01A` clinic applicant completes and submits an onboarding form with required secured files; submission creates no active tenant access.
- `TEN-01B` platform operator directly registers an application when onboarding is handled internally.
- `TEN-01C` platform operator resolves platform-managed document requirements, reviews each evidence item, approves/rejects the application and only then provisions the tenant and first owner.
- `TEN-02` suspend, reactivate or close a tenant through separately authorized control-plane workflow without silently deleting regulated records.
- `TEN-03` tenant owner configures legal entity, branches and defaults after activation and delegates branch-configuration permission to selected administrators.
- `TEN-04` authorized branch administrator creates and activates allow-listed branch branding/numbering overrides with version, concurrency and audit controls; provider-gated communication identities remain pending until externally verified.
- `TEN-05` platform operator creates, versions, activates and retires onboarding document types but does not routinely approve tenant branch configuration.
- `IAM-01` invite/activate/revoke users and assign branch-scoped roles.
- `IAM-02` approve privileged grants and review access/audit evidence.
- `CAT-01` configure specialties, services, prices and resource categories.

## Patient and relationship workflows

- `PAT-01` register temporary/full patient and verify contact.
- `PAT-02` search with masking, detect possible duplicate and perform reviewed merge.
- `PAT-03` manage contacts, representative, consent, alerts and documents.
- `PAT-04` patient views/corrects permitted profile data and communication preferences.

## Scheduling, resources and queues

- `SCH-01` configure provider/resource availability, recurrence and exceptions.
- `SCH-02` search, hold and book provider plus required room/machine/bed category atomically.
- `SCH-03` reschedule, cancel, waitlist, no-show and follow-up with lineage.
- `QUE-01` check in a booked or walk-in patient and issue queue ticket.
- `QUE-02` call, hold, skip, transfer, prioritize and complete a ticket with reasoned exceptions.
- `IMG-01` move an X-ray/CT/MRI order through arrival, preparation, scan, quality, reporting and release.

## Clinical workflows

- `ENC-01` open encounter from authorized booking/walk-in.
- `ENC-02` capture complaint, history, allergies, vitals, findings, diagnosis, procedures and plan.
- `ENC-03` draft, sign and amend an encounter without overwriting signed content.
- `RX-01` prescribe, issue, amend/cancel and share a prescription.
- `INV-01` order investigation, collect/perform, enter, verify and release result.
- `PHY-01` complete physiotherapy assessment with pain, range-of-motion, strength and functional baseline.
- `PHY-02` approve a goals-based treatment plan, schedule a course of sessions and record consent/precautions.
- `PHY-03` document each therapy session, interventions/modalities, exercise program, response and next plan.
- `PHY-04` reassess outcomes, revise the plan and discharge/refer with a versioned summary.
- `ORT-01` complete orthopaedic history/examination with body site, laterality, injury and neurovascular/red-flag findings.
- `ORT-02` order/review imaging or investigation, record diagnosis and create medication/procedure/referral/follow-up plan.
- `ORT-03` record approved clinic procedure and post-procedure instructions without treating planned surgery as completed care.

## Billing workflows

- `BIL-01` create estimate/invoice using effective price and tax snapshots.
- `BIL-02` approve discount, collect split payments, allocate and issue receipt.
- `BIL-03` refund/reverse with authority, reason and immutable lineage.
- `BIL-04` cashier close and branch/day reconciliation.

## Communications, reports and printing

- `COM-01` render and deliver approved email/SMS/WhatsApp/push notification with consent and retry rules.
- `REP-01` run authorized operational report in Admin or Portal.
- `REP-02` Admin schedules, monitors and distributes an approved report.
- `PRN-01` browser/PDF print normal output.
- `PRN-02` submit and acknowledge an agent print for receipt, queue slip or label.

## Cross-workflow exception catalog

Each workshop must include duplicate submission, stale state, revoked user/scope, wrong branch, patient merge during workflow, provider/resource unavailability, queue transfer, unsigned clinical draft, corrected result, payment callback duplication, Worker outage, provider outage, report timeout, printer offline and cutover rollback.

Detailed state rules belong in [Business Rules and State Machines](15-business-rules-and-state-machines.md); testable requirements belong in [Functional Requirements and Acceptance](16-functional-requirements-and-acceptance.md). Specialty detail is in [Physiotherapy and Orthopaedics Preparation](24-physiotherapy-orthopaedics-preparation.md).
