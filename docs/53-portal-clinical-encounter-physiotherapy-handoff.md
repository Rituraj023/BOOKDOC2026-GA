# 53 — Portal Clinical Encounter and Physiotherapy Handoff

Owner: Clinical + Physiotherapy + Portal + Blazor UI + Client + security  
Status: Verified role-facing reference slice  
Verified: 2026-08-27

## Outcome

The Portal now connects a practitioner’s assigned Booking to the common Encounter and the Physiotherapy workspace without requiring users to copy protected identifiers. The role-facing path is:

```text
Assigned confirmed Booking
  -> append-only Encounter draft/revisions
  -> verified Practitioner signs persisted latest revision
  -> signed PHYSIOTHERAPY Encounter
       +-- start policy-neutral care plan
       \-- continue open care plan / prefill treatment Encounter
```

This remains policy-neutral. Specialty templates, approved measures, red-flag logic, countersigning and clinical decision support are still governed by DOC-024.

## Practitioner-scoped agenda

`GET /api/v1/branches/{branchId}/encounters/agenda?date={local-date}&take={1..100}` provides a single-day bounded agenda. It requires `Encounters.View` and returns only confirmed Bookings matching the signed-in identity’s:

- active Practitioner profile and active Identity user;
- active Person Stakeholder;
- currently verified credential;
- effective branch and Service assignment;
- assigned bookable resource when the assignment names one.

An authorized but unassigned user receives an empty agenda rather than branch-wide patient data. The date is interpreted using the Branch time zone.

The response contains protected Booking/Patient/Service/Encounter/care-plan IDs, safe business numbers, Patient display name/number, service name/code, appointment time and lifecycle status. It excludes contact details and every Encounter, care-plan, session and outcome narrative value.

## Encounter workspace

The new `/clinical/encounters` Portal screen provides:

- date selection and assigned agenda;
- start from the selected protected Booking;
- common, dynamic specialty/template identity rather than a hard-coded protocol;
- append-only draft revisions with optimistic versioning;
- signing only with `Encounters.Sign` and backend Practitioner eligibility;
- full authorized immutable Encounter history and content hashes;
- safe error/session handling through the existing Portal boundary.

The shared `EncounterDraft` model separates minimum save requirements from sign requirements. The UI disables signing while form changes are unsaved, ensuring it cannot silently sign an older persisted revision. The backend remains authoritative for assessment/plan requirements, concurrency and signer eligibility.

## Physiotherapy handoff

After signing, `ClinicalHandoffActionResolver` offers a Physiotherapy action only when the latest Encounter specialty is exactly `PHYSIOTHERAPY`:

- no linked/open plan: open Portal Physiotherapy with the signed Encounter prefilled for care-plan creation;
- linked/open plan: open the protected plan and prefill the signed Encounter for treatment-session entry;
- another specialty: review the signed Encounter without inventing a specialty workflow.

The action is also filtered by `Physiotherapy.CarePlans.View`; starting a plan additionally requires `Physiotherapy.CarePlans.Manage`. All API-side permissions and eligibility checks still apply.

Because public ID protection is probabilistic, refreshed agenda selection uses the stable Booking number for presentation matching. Current protected tokens are used for every API request and are never treated as durable UI equality keys.

## Shared UI

`BookDoc2026.Blazor.UI` now contains:

- `ClinicalAgendaWorklist`;
- `EncounterDraftForm` and testable `EncounterDraft` state;
- `EncounterHistory`;
- `ClinicalHandoffActionResolver`.

These components contain presentation/state rules only. Authorization, scope, domain transitions and persistence remain in API/Application/Domain/Infrastructure.

## Verification

The complete Debug suite is green:

```text
Unit:          90 passed
Architecture:   9 passed
Integration:   35 passed
Total:        134 passed, 0 failed
```

New tests cover save-versus-sign requirements, reset/load behavior, allowed handoff states, empty agenda for an unassigned actor, assigned Practitioner agenda visibility, protected IDs, absence of clinical narrative in the agenda, Encounter status and linked care-plan state. All earlier immutability, concurrency, eligibility and cross-tenant tests remain green.

No database schema change or migration was required.

## Deliberately deferred

- clinician-approved templates, dictionaries, safety prompts and validation ranges;
- Queue/check-in state as a prerequisite for starting an Encounter;
- Booking completion/no-show and Encounter timing policy;
- signed amendment/countersigning Portal UI;
- multiple simultaneous/open care-plan policy for one Patient and Service;
- exercise, discharge and patient-delivery documents;
- Portal realtime agenda invalidation;
- MAUI/offline clinical use;
- broad accessibility, clinician UAT and approved specialty-template evidence. The synthetic browser fixture is completed in [DOC-054](54-clinical-development-fixtures-and-browser-acceptance.md).

## Achievement effect and next goal

DOC-033 row 7 advances from 80% to 85% because Portal now connects an assigned practitioner agenda, common clinical documentation and Physiotherapy delivery. Row 9 advances from 75% to 80% because reusable agenda, Encounter form/history and handoff state now serve a real workflow. The exact twenty-row average moves from 74.00% to **74.50%**.

The recommended browser goal is completed in [DOC-054](54-clinical-development-fixtures-and-browser-acceptance.md), and the Encounter-linked Investigation Order/Queue handoff is completed in [DOC-055](55-encounter-investigation-order-imaging-queue-handoff.md). The next goal is its narrowly authorized radiology ordered-work worklist and isolated clinician-to-technician browser acceptance, without result-policy invention.
