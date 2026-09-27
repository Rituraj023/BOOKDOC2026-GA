# 52 — Portal Physiotherapist Workspace

Owner: Physiotherapy + Portal + Blazor UI + Client + security  
Status: Verified role-facing reference slice  
Verified: 2026-08-26

## Outcome

The authenticated Portal now provides a permission-composed Physiotherapist workspace on the DOC-051 clinical foundation. An authorized practitioner can discover branch care plans, review immutable history, create/revise/activate/close plans, append signed-Encounter treatment sessions and record dynamic outcome observations.

The workspace remains policy-neutral. It does not define a clinical protocol, approved assessment template, proprietary outcome scale, red-flag decision, countersigning rule or offline workflow.

## Journey

```text
Portal dashboard
  -> bounded branch care-plan worklist
  -> open protected care-plan detail
  -> review revision/session/outcome history
  -> permitted command
       +-- create/revise/activate/complete/discontinue
       +-- record signed-Encounter treatment session
       +-- record dynamic outcome observation
  -> reload authoritative API state
```

The home dashboard and navigation expose the workspace only with `Physiotherapy.CarePlans.View`. Each command is then shown independently for `CarePlans.Manage`, `Sessions.Record` or `Outcomes.Record`. API authorization, durable branch scope and Practitioner eligibility remain authoritative even if a client is modified.

## Bounded worklist API

`GET /api/v1/branches/{branchId}/physiotherapy/care-plans?take={1..100}` adds the missing clinical worklist query. It returns only:

- protected care-plan, Patient and Service identifiers;
- care-plan number and status;
- Patient display name and patient number;
- revision number and review date;
- session count, last-session time and aggregate version.

It excludes contact details, goals, precautions, session narrative, adverse-event detail and outcome values. Full clinical content is loaded only after the user opens one authorized care plan.

Protected IDs are probabilistic. The UI never compares separately generated public tokens for equality; it uses the branch-unique care-plan number only for presentation selection and uses the current protected token for each API call.

## Shared Blazor UI

Reusable components in `BookDoc2026.Blazor.UI` provide:

- `PhysiotherapyWorklist` for responsive branch summary selection;
- `PhysiotherapyStatusBadge` for consistent Draft/Active/Completed/Discontinued presentation;
- `PhysiotherapyHistory` for revisions, signed sessions and outcome series;
- `PhysiotherapyOutcomeSeriesBuilder` for chronological like-for-like grouping.

Outcome comparison groups only matching measure code, tool version, unit, body site and laterality. The UI accepts dynamic clinic-approved codes instead of shipping an unapproved scale catalog.

## Portal behavior

- Search and status filters operate on the bounded 100-row result.
- Creation requires a protected signed Physiotherapy Encounter ID and the minimum policy-neutral plan content.
- Active-plan revisions require a reason and submit the latest optimistic version.
- Session entry requires a protected signed treatment Encounter, mandatory response/intervention/tolerance/next-plan content and conditional adverse-event detail.
- Outcome entry supports optional session linkage, context/measure/tool/unit, decimal value, body/laterality and observed time.
- Completion/discontinuation requires a reason.
- Every successful mutation reloads the bounded list and keeps the selected course by care-plan number.
- Remote safe errors and session expiry use the existing Portal handling boundary.

## Verification

The complete Debug suite is green:

```text
Unit:          84 passed
Architecture:   9 passed
Integration:   35 passed
Total:        128 passed, 0 failed
```

New evidence verifies like-for-like outcome grouping/order and the permission-protected worklist projection. Integration evidence confirms the worklist contains protected identity and operational summaries but no care-plan narrative. Existing tests continue to prove mutation permissions, Practitioner eligibility, optimistic concurrency, immutability and cross-tenant rejection.

Portal and API builds pass with zero warnings and errors. This slice does not claim real-browser, accessibility, clinician UAT or deployed-environment acceptance.

## Deliberately deferred

- approved assessment/body-region templates, measures, ranges and warnings;
- graphical trend interpretation or clinical decision support;
- clinician/supervisor countersigning and closed-record correction policy;
- exercise/home-program and discharge-document generation;
- patient-facing and MAUI Physiotherapy journeys;
- offline clinical data, push and clinical-content notification policy;
- Queue/check-in-to-Encounter timing remains deferred; the assigned Booking/Encounter handoff is now implemented in [DOC-053](53-portal-clinical-encounter-physiotherapy-handoff.md);
- broad accessibility audit and clinical-owner UAT. Synthetic real-browser fixture acceptance is completed in [DOC-054](54-clinical-development-fixtures-and-browser-acceptance.md).

## Achievement effect and next goal

DOC-033 row 7 advances from 75% to 80% for another permission-scoped Portal role journey. Row 9 advances from 70% to 75% because three real reusable components and a testable outcome-series model now serve the Portal through `Blazor.UI`. The exact twenty-row average moves from 73.50% to **74.00%**.

Recommended next: implement a **policy-neutral Portal clinical Encounter and Physiotherapy handoff** so an eligible practitioner can open the scheduled patient/Booking, draft and sign the common Encounter, then start or continue the linked care plan without manually copying protected IDs. Follow it with a development fixture and real-browser acceptance; keep specialty templates behind DOC-024 approval.
