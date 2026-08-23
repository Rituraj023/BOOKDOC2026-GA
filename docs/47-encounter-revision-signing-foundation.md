# 47 — Encounter Revision and Signing Foundation

Owner: Clinical + backend + security + migration  
Status: Verified policy-neutral reference slice  
Reviewed: 2026-08-23

## Outcome

BOOKDOC2026 now has a common clinical Encounter foundation linked to a confirmed generalized Booking. It supports append-only draft history, immutable signed revisions and reasoned signed amendments without overwriting earlier clinical text.

This is intentionally not a completed Physiotherapy or Orthopaedics protocol. Delhi clinical representatives have not yet approved specialty field sets, outcome tools, red-flag handling, professional/countersigning rules, procedures, prescriptions or release documents. Those remain blocked by DOC-024 rather than being guessed in code.

## Legacy evidence and redesign

The legacy `AssessmentInfo`, `PhisioAssessmentInfo`, `AssessmentVitalsignInfo`, `AssessmentProgressionInfo` and `AssessmentTreatmentInfo` prove useful requirements: Booking-linked assessment, complaints, history, examination, diagnosis/assessment, instructions, body site/laterality, progression, vitals and treatment authorship.

The EF6 inheritance, one-row-per-Booking dependent keys, generic editable CRUD, free-form user linkage, local timestamps and delete/update services were not ported. The target separates the Encounter identity from immutable revisions, uses UTC, protected public IDs, explicit permissions, concurrency, tenant-qualified relationships and audit metadata that excludes clinical narrative.

## Implemented model

`ClinicalEncounter` owns tenant, branch, Booking, Patient, service, encounter number, `Draft | Signed` status, latest-revision pointer, latest signer/time and optimistic version.

`EncounterRevision` is append-only and owns:

- sequential revision number and `Draft | Signed | Amendment` kind;
- parent-revision lineage;
- author identity and timestamps;
- specialty/template key and version;
- generic complaint, history, examination, assessment, plan and instructions;
- optional body site and laterality code;
- deterministic SHA-256 content hash;
- amendment reason and signed timestamp.

The lifecycle is:

```text
confirmed Booking
  -> Encounter Draft + revision 1
  -> zero or more append-only Draft revisions
  -> new immutable Signed revision
  -> zero or more new immutable Amendment revisions with reason
```

Signing requires a non-empty chief complaint, assessment and plan. [DOC-048](48-practitioner-credential-and-assignment-foundation.md) now additionally requires an active Identity-linked Practitioner, current verified credential and effective assignment for the Encounter branch/service. The same eligibility rule applies to a signed amendment. A signed Encounter cannot receive another draft revision. Amendment is an atomic signed replacement revision: it keeps the original signed revision and hash intact, links to its parent and requires a 5–500 character correction reason.

## API and authorization

| Operation | Endpoint | Permission |
|---|---|---|
| Start Encounter draft | `POST /api/v1/branches/{branchId}/encounters` | `Encounters.Drafts.Manage` |
| Read complete revision history | `GET /api/v1/branches/{branchId}/encounters/{encounterId}` | `Encounters.View` |
| Append draft revision | `POST /api/v1/branches/{branchId}/encounters/{encounterId}/draft-revisions` | `Encounters.Drafts.Manage` |
| Sign latest draft | `POST /api/v1/branches/{branchId}/encounters/{encounterId}/sign` | `Encounters.Sign` |
| Append signed amendment | `POST /api/v1/branches/{branchId}/encounters/{encounterId}/amendments` | `Encounters.Amend` |

The four permissions are branch-assignable. Encounter and revision DTO IDs are tenant/type-bound protected strings; author/signer IDs use the protected Identity-subject purpose. The typed Client exposes all five operations.

Every change records action, revision identity/number/kind and content hash in the operational audit. Complaint, history, examination, assessment, plan and instructions are deliberately excluded from the audit payload.

## Persistence

Migration `20260823111410_EncounterRevisionFoundation` adds:

- `clinical.encounter` with one Encounter per tenant/branch/Booking;
- `clinical.encounter_revision` with unique sequential revisions and tenant-qualified parent lineage;
- eight append-only platform/clinic role permission claims.

Database foreign keys prove Booking, Patient, service and branch ownership. `BookDocDbContext` refuses modification or deletion of any tracked Encounter revision. The migration is applied locally and EF reports no pending model changes.

## Automated evidence

Four domain tests prove draft lineage, sign-as-new-revision behavior, required signing content, reasoned amendment with original-hash preservation and stale-version rejection.

One full API integration test proves:

- real tenant, branch, Patient, service/resource, confirmed Booking and Encounter persistence;
- negative authorization for start, sign and amend;
- duplicate-Encounter rejection for a Booking;
- protected Encounter identifiers;
- draft revision, signing and amendment history;
- unchanged original signed hash after amendment;
- stale sign conflict;
- DbContext append-only delete protection;
- cross-tenant protected-ID rejection.

## Explicitly deferred clinical decisions

- specialty-specific credential authorities, supervisory/countersigning policy and temporary coverage;
- author, supervisor, countersigner, amendment and discharge role matrix;
- Encounter timing relative to check-in, queue and Booking completion/no-show;
- approved Physiotherapy assessments, care plans, measures, sessions and exercise programs;
- approved Orthopaedic body-region/laterality dictionaries, procedures and consent;
- red-flag/escalation prompts and mandatory fields;
- structured diagnosis, vitals, procedures, prescription and investigation aggregates;
- signed PDF/DOCX generation, storage, patient release and retention;
- offline clinical drafting policy;
- representative legacy migration and clinician/UAT acceptance.

## Achievement effect

This Encounter slice itself did not raise the twenty-row score. The subsequent Practitioner evidence in DOC-048 raises the current exact score to **71.50%** by advancing central Stakeholder-role reuse; it does not claim specialty, UI or production acceptance.

The next clinical step remains approval of DOC-024 specialty content and author/supervisor/countersigner policy. The next recommended independent implementation slice is Billing invoice/allocation/receipt foundation.
