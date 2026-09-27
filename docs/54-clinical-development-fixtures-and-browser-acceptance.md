# 54 — Clinical Development Fixtures and Browser Acceptance

Owner: Clinical + Physiotherapy + Portal + shared UI + Identity + QA  
Status: Verified local browser reference journey  
Reviewed: 2026-08-28

## Outcome

The Portal clinical vertical is now proven in a rendered browser against the real local API and SQL Server database:

```text
Assigned clinician
  -> permission-composed Portal dashboard
  -> practitioner/credential/assignment-filtered agenda
  -> common Encounter draft
  -> append-only draft revision
  -> signing only after saved complete assessment + plan
  -> direct signed-Encounter Physiotherapy handoff
  -> draft and active care plan

Unassigned clinician
  -> same clinical permissions and branch scope
  -> no practitioner assignment
  -> empty agenda
```

This is synthetic engineering evidence. It is not clinician UAT, approval of a Physiotherapy template, a medical-policy decision or authorization for production clinical use.

## Removable fixture extension

`BookDoc2026.DevelopmentFixtures` now extends the existing guarded browser fixture with:

- an assigned clinician Identity user and an unassigned clinician Identity user;
- one narrowly scoped clinical role containing Encounter and Physiotherapy permissions;
- one central Person Stakeholder for the synthetic physiotherapist;
- an active Practitioner profile linked to that Identity subject;
- one verified, current synthetic credential;
- one active branch/service/resource assignment;
- a Physiotherapy service, practitioner resource category and exclusive resource;
- an X-ray catalog Service, canonical `XRAY` imaging-modality category, required relationship and matching service point;
- a confirmed resource-allocated Booking for the synthetic Patient on the next Delhi-local date.

No fixture runs during API, Portal, Admin, Worker or AppHost startup. The command retains all DOC-045 protections: explicit action and confirmation, Development environment, externally supplied temporary password, `_Dev` database suffix, reachable/current migration checks, refusal to overwrite an existing fixture and retry-aware transactions.

Removal was expanded in dependency order for Investigation events/orders/Queue tickets, Physiotherapy outcomes/sessions/plans, Encounter revisions/aggregates, Booking allocations/Bookings/reservations/holds, Practitioner assignments/credentials/profiles and Catalog/Resource rows before existing Patient/Stakeholder/tenant and Identity cleanup. DOC-055 additionally corrected resource status/capability/requirement deletion before resources/categories/services after a real SQL foreign-key rehearsal exposed the ordering dependency. A second removal remained idempotent.

No credential, access token, protected identifier, patient narrative or provider secret is committed to the repository or this document.

## Rendered acceptance evidence

Validated in isolated local browser sessions on 2026-08-27 and 2026-08-28:

1. The assigned clinician dashboard exposed Clinical and Physiotherapy capabilities.
2. The default-date agenda excluded the next-day Booking; selecting its Delhi-local date returned exactly the assigned confirmed Booking.
3. The agenda showed only the bounded Patient/Booking/Service projection and no Encounter narrative.
4. Required draft fields enabled `Start Encounter` while typing.
5. Starting produced revision 1 and refreshed the agenda to `Draft`.
6. Changing the plan disabled signing until `Append draft revision` persisted revision 2.
7. The immutable history retained both draft contents and hashes.
8. Signing created a new signed revision and exposed the authorized Physiotherapy handoff.
9. The handoff populated the protected Encounter ID without copying it manually.
10. Required care-plan fields enabled creation while typing; the resulting draft was activated and appeared in the worklist/history.
11. A separate user with the same permissions and branch scope but no Practitioner chain saw zero agenda rows for the same date.

## Browser defects corrected

Rendered use found presentation defects that backend and state-object tests did not expose:

1. `EncounterDraftForm` mutated its shared model without notifying the parent page, so command buttons did not re-evaluate. The component now binds on input and emits a change callback; both start and revise views use it.
2. Physiotherapy command fields used change-only binding, leaving create/revise/session/outcome/closure buttons stale while a user typed. Required string fields now bind on input.
3. Encounter history rendered a Razor expression literally and then duplicated a hard-coded version prefix. It now renders the approved template-version value exactly as stored.

Domain authorization, tenant scope, Practitioner eligibility, protected-ID decoding and persistence remain in API/Application/Domain/Infrastructure. These UI changes do not move business rules into Razor components.

## Verification

```text
Development fixture create:                 passed
Assigned clinician browser journey:          passed
Unassigned clinician negative browser case:  passed
Fixture transactional removal:               passed
Fixture idempotent second removal:            passed
Portal Release build:                         passed, 0 warnings / 0 errors
Unit tests:                                   90 passed
Architecture tests:                            9 passed
Integration tests:                            35 passed
Total automated tests:                       134 passed, 0 failed
EF model drift:                               none
Temporary tenant/users/roles/clinical data:   removed
Temporary local fixture credential file:      removed
```

A solution-wide `--no-restore` multi-target build was not used as acceptance evidence: the existing MAUI assets lacked the `maccatalyst-arm64` target and the build continued into lengthy Android AOT work before being stopped. The affected Portal and fixture projects build cleanly; restoring and rationalizing all MAUI runtime identifiers remains separate mobile build-pipeline work.

## Achievement effect

DOC-033 row 9 advances from **80% to 85%** because the shared Encounter form/history and Physiotherapy form state now pass a real rendered workflow and the discovered interaction defects are corrected. Row 20 advances from **75% to 80%** because the clinical vertical now has isolated assigned/unassigned browser evidence plus repeatable fixture cleanup.

The twenty-row score gains 10 points: `1500 / 20 = 75.00%`.

- exact architecture-achievement completion: **75.00%**;
- rounded headline completion: **75%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

No score is awarded for clinician/clinic UAT, approved specialty templates, broad WCAG evidence, CI browser automation, production security/operations, representative migration or mobile-device acceptance.

## Continuation

The policy-neutral Encounter-linked Investigation Order/Queue handoff and technician ordered-work projection are completed in [DOC-055](55-encounter-investigation-order-imaging-queue-handoff.md) and [DOC-056](56-radiology-technician-ordered-worklist.md). [DOC-059](59-portal-radiology-execution-quality-workspace.md) subsequently completes a new three-session clinician-to-operator-to-independent-reviewer browser run. That later evidence is not retroactively claimed by this historical DOC-054 checkpoint.
