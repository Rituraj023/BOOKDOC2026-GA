# 15 — Business Rules and State Machines

## General rules

- All state-changing requests use authorization, durable scope, optimistic concurrency where applicable and idempotency for externally repeatable commands.
- State history is append-only for clinical, financial, booking, queue, job and print transitions.
- Cancellation, reversal, correction and amendment preserve the previous accepted state and actor/reason/time.
- Tenant and branch cannot be changed by editing a record after creation; controlled transfer creates lineage.

## Proposed state models

| Aggregate | Proposed states | Critical rules |
|---|---|---|
| Appointment | Requested, Held, Confirmed, CheckedIn, InService, Completed, Cancelled, NoShow | holds expire; required resources commit atomically; reschedule links old/new |
| Resource reservation | Held, Reserved, Consumed, Released, Cancelled | capacity cannot be exceeded; bed reservation is not inpatient occupancy |
| Queue ticket | Waiting, Called, Held, InService, Transferred, Completed, Cancelled, NoShow | one active caller; priority/skip/transfer needs permission and reason |
| Encounter | Draft, ReadyToSign, Signed, Amended, EnteredInError | signed content immutable; amendment creates new version |
| Prescription | Draft, Issued, Amended, Cancelled | only authorized practitioner issues; issued snapshot retained |
| Investigation order | Requested, Accepted, Cancelled, Completed | clinical request only; completion follows approved obligations and is never copied from Queue |
| Radiology study | Registered, InProgress, Acquired, QualityAccepted, RepeatRequired, Aborted, Cancelled | acquisition and quality are explicit; every attempt/review is retained; Queue completion changes neither |
| Radiology report | Draft, PreliminarySigned, FinalSigned, AmendedSigned | preliminary is optional; signed versions immutable; amendment supersedes without overwrite |
| Critical-result communication | Required, Attempted, EscalationDue, Acknowledged | classification is clinician-owned; delivery/open is not acknowledgement; durable escalation survives Worker restart |
| Radiology report release | ClinicalReleased, PatientReleased, Superseded | signed version and immutable snapshot required; audiences and permissions remain separate |
| Physiotherapy care plan | Draft, Active, OnHold, Completed, Discontinued | goals/measures versioned; each session has author/time/interventions/response; reassessment never overwrites baseline |
| Orthopaedic procedure plan | Proposed, ConsentPending, Scheduled, Completed, Cancelled, Referred | body site/laterality and consent confirmed; completion requires a procedure record, not calendar status alone |
| Invoice | Draft, Issued, PartPaid, Paid, Voided, Refunded | issued number/tax/price snapshot immutable; void/reversal controlled |
| Payment | Initiated, Pending, Confirmed, Failed, Reversed, Refunded | provider callback idempotent; allocation never exceeds confirmed amount |

DOC-049 implements the direct clinic-receipt subset as `Confirmed -> FullyAllocated`, with partial allocation represented by the confirmed Payment balance. Provider `Initiated/Pending/Failed`, reversal and refund remain unimplemented until their separate command/policy matrices are approved. Invoice issue currently uses `Issued -> PartPaid -> Paid`; void/credit-note behavior is deferred rather than inferred.
| Message job | Queued, Claimed, Succeeded, RetryScheduled, DeadLettered, Cancelled | bounded retry; external ambiguity visible; no SignalR execution |
| Report execution | Queued, Running, Completed, Failed, Expired, Cancelled | authorization at request and retrieval; immutable official snapshot selective |
| Print job | Queued, Claimed, Printed, Failed, Expired, Cancelled | branch-bound agent; claim/acknowledgement idempotent; reprint explicit |

## Rules requiring clinic approval

- appointment overbooking, hold duration, cancellation window and no-show policy;
- resource pooling, duration buffers and emergency priority;
- queue priority classes, SLA timers and public display masking;
- who may create walk-ins and amend signed records; X-ray/CT acquisition and technical-quality defaults RAD-01 through RAD-09 are provisionally implemented for development in [DOC-058](58-radiology-study-acquisition-quality-foundation.md) but require clinical/radiology/compliance validation before live use, while interpretation, critical communication and release remain gated by RAD-10 through RAD-24 in [DOC-057](57-xray-ct-execution-result-policy-decision-pack.md);
- discount/refund thresholds and cashier-close policy;
- prescription numbering/signature requirements by specialty;
- approved physiotherapy outcome measures, session correction rules, package/session consumption and exercise-plan sharing;
- orthopaedic laterality/site verification, red-flag escalation, procedure consent and external surgery/referral boundary;
- retention duration for drafts, clinical records, invoices, receipts, reports and print payloads;
- consent/notice wording, quiet hours and urgent communication exceptions.

No state machine is implementation-ready until transitions, invalid transitions, permissions, side effects, concurrency behavior and audit events have approved examples.
