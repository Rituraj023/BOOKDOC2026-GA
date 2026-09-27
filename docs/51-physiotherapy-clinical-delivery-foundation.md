# 51 — Physiotherapy Clinical Delivery Foundation

Owner: Clinical + Physiotherapy + backend + security + migration  
Status: Verified policy-neutral backend reference slice  
Verified: 2026-08-26

## Outcome

BOOKDOC2026 now has a tenant- and branch-scoped Physiotherapy delivery foundation built on the common signed Encounter and verified Practitioner boundaries. It preserves versioned care plans, immutable treatment sessions and immutable outcome observations without copying the legacy Xamarin/desktop architecture or inventing unapproved clinical protocols.

This is implementation evidence, not approval of a Delhi clinical template, treatment protocol, outcome scale, red-flag rule, countersigning rule or release document.

## Legacy behavior retained as requirements

The legacy BookDoc applications showed the need to record assessment-derived plans, treatment progression and repeated visits against the same patient course. Those behaviors are retained as requirements. Legacy entities, repositories, direct database access, doctor-only assumptions and mutable note structures were not ported.

The redesigned boundary instead:

- uses the central Person/Patient, Service, Booking, Encounter and Practitioner records;
- requires a signed `PHYSIOTHERAPY` Encounter before a care plan can start;
- requires the acting user to be an active, verified Practitioner assigned to the branch and service;
- keeps clinical text out of operational audit payloads;
- uses protected public strings while retaining signed numeric keys internally;
- permits dynamic measure codes, tool versions, units, body site and laterality without hard-coding a proprietary or clinic-specific scale.

## Aggregate and lifecycle

```text
Signed Physiotherapy Encounter
             |
             v
       Draft Care Plan --activate--> Active --complete--> Completed
              |                         \\--discontinue--> Discontinued
              |
              +-- append revision        +-- immutable sessions
                                         +-- immutable outcomes
```

- A care plan belongs to one tenant, branch, Patient, Service and initial signed Encounter.
- Each edit appends a numbered revision. An Active-plan revision requires a change reason and expected aggregate version.
- Activation, revision and closure use optimistic concurrency.
- Completion and discontinuation require a reason and prevent further sessions or observations.
- A treatment session links a separate signed Encounter and its Booking to the care plan, carries a monotonic sequence, author, performed time and SHA-256 content hash.
- Adverse-event details are mandatory exactly when the adverse-event flag is true.
- An outcome observation is append-only and may optionally link to a treatment session. Its measure code, tool version, context, decimal value, unit, body site, laterality, author and observation time remain intact for like-for-like comparison.

## Permissions and API

All operations also enforce durable branch scope and Practitioner eligibility.

| Permission | Operation |
|---|---|
| `Physiotherapy.CarePlans.View` | Read one scoped care plan and its history |
| `Physiotherapy.CarePlans.Manage` | Create, revise, activate, complete or discontinue |
| `Physiotherapy.Sessions.Record` | Append a signed-Encounter treatment session |
| `Physiotherapy.Outcomes.Record` | Append an outcome observation |

The typed API/Client routes are rooted at `/api/v1/branches/{branchId}/physiotherapy/care-plans`. They support create/read, revision, activation, completion, discontinuation, session recording and outcome recording. Branch, care-plan, Encounter and optional session identifiers are decoded as resource-kind- and tenant-bound public IDs.

## Persistence

The pending Physiotherapy migration was consolidated before source control with the next Clinical slice. Migration `20260828004550_PhysiotherapyAndInvestigationClinicalDelivery` adds the four Physiotherapy tables below plus the separately documented DOC-055 Investigation/Queue structures:

| Table | Ownership and rule |
|---|---|
| `clinical.PhysiotherapyCarePlans` | Mutable lifecycle/version root with Patient, Service and initial Encounter references |
| `clinical.PhysiotherapyCarePlanRevisions` | Append-only versioned content, parent, author, reason and hash |
| `clinical.PhysiotherapyTreatmentSessions` | Append-only care-plan/Encounter/Booking session evidence |
| `clinical.PhysiotherapyOutcomeObservations` | Append-only dynamic measurement evidence |

Tenant query filters apply to every table. Composite tenant/branch relationships protect care-plan-owned history. The DbContext rejects modification or deletion of revisions, sessions and outcomes. The migration was applied to the local development database and EF reports no intended legacy-table dependency.

## Security and audit

- The API uses four independently assignable permissions rather than role names.
- Cross-tenant protected identifiers fail before data access; branch scope is checked again in the application service.
- An eligible Practitioner is checked for every mutation, not only care-plan creation.
- Audit events contain identifiers, lifecycle codes, version/sequence and content hashes; they do not contain goals, precautions, session narrative, adverse-event narrative or observed clinical values.
- Duplicate create and stale-version requests are rejected; immutable clinical history is not overwritten to resolve a conflict.

## Verification

The complete Debug solution test run is green:

```text
Unit:          83 passed
Architecture:   9 passed
Integration:   35 passed
Total:        127 passed, 0 failed
```

New evidence covers revision numbering and reasons, wrong specialty/unsigned Encounter rejection, stale versions, session/adverse-event invariants, outcome validation, closure behavior, Practitioner eligibility, separated read/write permissions, append-only database protection and cross-tenant rejection.

## Deliberately deferred

- clinician-approved assessment forms, body-region fields, red-flag prompts and treatment protocols;
- approved outcome-tool catalog, permissible units/ranges and comparison/trend UX;
- supervisory/countersigning, corrections after closure and external-referral rules;
- exercise library, media, home program, discharge summary and patient delivery;
- package consumption/refund coupling and final finance rules;
- Physiotherapist Portal/MAUI journey, offline policy and device acceptance;
- representative legacy data mapping, clinical UAT and production migration rehearsal.

The readiness gate in [Physiotherapy and Orthopaedics Preparation](24-physiotherapy-orthopaedics-preparation.md) remains open for these policy-bearing decisions.

## Achievement effect and next goal

This additional API-composed clinical module advances DOC-033 row 3 from 90% to 95%, moving the exact twenty-row average from 73.25% to **73.50%**. It does not raise the specialty to production readiness or change the scores for Portal, Mobile, migration or operational acceptance.

Recommended next: build the permission-scoped **Portal Physiotherapist worklist and care-plan/session/outcome journey** with shared Blazor components and dynamic measure inputs. Keep final templates, scales, clinical warnings and offline/mobile behavior behind DOC-024 owner approval.
