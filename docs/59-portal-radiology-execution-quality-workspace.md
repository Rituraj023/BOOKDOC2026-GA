# 59 — Portal Radiology Execution and Quality Workspace

Owner: Radiology + Clinical Investigations + Portal + shared UI + Identity + QA  
Status: Verified local role-facing reference journey; tenant live use gated  
Reviewed: 2026-08-29

## Outcome

The policy-bounded Radiology Slice A from [DOC-057](57-xray-ct-execution-result-policy-decision-pack.md) and [DOC-058](58-radiology-study-acquisition-quality-foundation.md) is now usable through the authenticated Portal:

```text
Eligible operator
  -> bounded ordered-work list
  -> register one Study for the Investigation Order
  -> start acquisition
  -> choose only matching-modality, service-capable equipment
  -> record one immutable acquisition attempt

Independent quality reviewer
  -> same branch-scoped ordered-work list
  -> no operator or Queue-transition commands
  -> review the latest acquisition attempt
  -> accept technical quality or require a repeat

Both sessions
  -> reload authoritative Study state after commands
  -> see immutable attempt/review history
  -> keep Queue, Study and Result state visibly separate
```

This is synthetic engineering evidence. It is not Delhi clinical/radiology/compliance approval, equipment regulatory approval, a diagnostic interpretation workflow or authorization for tenant live use.

## Preserved safety boundary

The workspace exposes performed-work facts only:

- the Queue ticket remains the operational patient-flow record;
- the Radiology Study remains the acquisition and technical-quality record;
- the Investigation Order remains the requested clinical intent;
- no interpreted Result, report version, critical communication, release command or patient document is created;
- `QualityAccepted` means only that an independent technical reviewer accepted the recorded attempt.

The rendered acceptance run finished with Queue status `Waiting` and Study status `QualityAccepted`. This deliberately proves that Study commands do not fabricate Queue or Result transitions.

## Implemented application boundary

### API and typed client

The existing Radiology API now includes a dedicated eligible-equipment query. It requires `Radiology.Acquisitions.Record`, branch scope and current operator eligibility before returning a minimal protected-ID/code/name option projection. Equipment must be active and available, belong to the Study branch, use the matching canonical X-ray/CT modality category, advertise the ordered service and satisfy any resource-bound service-point restriction.

The typed Client now supports Study lookup by Order or Study identity, registration, start, eligible-equipment lookup, acquisition recording and technical-quality review. Commands continue to use expected aggregate versions and caller-stable request identities.

### Shared Blazor UI

`BookDoc2026.Blazor.UI` owns the reusable Study status badge, execution workspace and command drafts. The components provide:

- bounded Order/Patient/Service/indication context;
- explicit Queue/Study/Result boundary language;
- permission-composed operator and reviewer commands;
- structured protocol, deviation, abort and review inputs;
- matching-equipment selection without exposing raw internal identifiers;
- immutable acquisition/review history;
- busy, empty, warning, completion and refresh states;
- stable request identity across an uncertain retry, rotated only after an authoritative success.

Business authorization, Practitioner eligibility, modality enforcement, version conflicts and self-review denial remain authoritative in API/Application/Domain. Razor visibility is usability defense in depth, not the security boundary.

### Portal orchestration

The Portal Radiology page now composes the ordered-work list and Study workspace. It retrieves current state after every successful command, provides an explicit manual reconciliation action for another session's change and does not infer Study state from Queue SignalR messages. Durable work is still performed by API/Application/Worker boundaries; SignalR is status invalidation only.

Cross-session Study invalidation is deliberately not claimed in this slice. The recommended follow-up adds privacy-safe Study status invalidation and reconnect evidence without moving commands into SignalR.

## Authorization and two-person control

The fixture role composition and browser run prove two distinct capability sets:

| Capability | Eligible operator | Independent reviewer |
|---|---:|---:|
| View ordered work and Study | Yes | Yes |
| Register/start Study | Yes | No |
| List eligible equipment/record acquisition | Yes | No |
| Review technical quality | No | Yes |
| Call/cancel Queue ticket | Yes in this fixture | No |

The API additionally proves that:

- a user with worklist permission alone cannot read or mutate a Study;
- permission without a current verified credential and effective assignment fails;
- the performing actor cannot review their own attempt;
- a reviewer cannot use the acquisition-only equipment query;
- cross-tenant protected identifiers fail closed;
- wrong-modality equipment fails even when it advertises the ordered service.

## Removable fixture extension

The guarded `BookDoc2026.DevelopmentFixtures` command now adds, only on explicit Development create:

- separate synthetic radiology operator and quality-reviewer identities/roles;
- central Person Stakeholders and eligible Practitioner profiles for both actors;
- current verified synthetic credentials and effective service assignments;
- canonical X-ray and CT modality categories;
- one matching X-ray device and one deliberately non-matching CT device;
- the existing synthetic clinician, signed-Encounter path, imaging catalog service and service point.

No fixture runs during normal host startup. No password, access token, signing key, protected identifier, patient narrative or provider secret is committed. Removal now deletes quality reviews, acquisition attempts, Study events and Studies before dependent Investigation/Queue/Clinical/Catalog/Stakeholder/Identity records. The completed acceptance run removed all fixture data, and a second remove confirmed the cleanup path is idempotent.

## Authenticated browser acceptance

Validated in three isolated local Portal sessions against the real API and local SQL Server on 2026-08-29:

1. The assigned synthetic clinician created and signed the common Encounter, requested the configured X-ray service and handed the Order to the configured X-ray Queue.
2. The operator dashboard exposed Radiology execution and the worklist returned exactly the handed-off Order.
3. The operator registered and started one Study; Queue status remained `Waiting`.
4. The equipment selector returned only the matching X-ray device. The deliberately service-capable CT device was absent.
5. The operator could record the acquisition but had no quality-decision command.
6. The reviewer could open the same acquired Study but had no start, acquisition, call or cancel command.
7. The reviewer accepted technical quality, producing Study version 4 and one immutable review linked to attempt 1.
8. The operator reconciled the authoritative state and saw `QualityAccepted` while the Queue still showed `Waiting`.
9. Both sessions showed that no interpretation, report or release had been created.
10. The clinician, operator and reviewer browser consoles contained no runtime errors.

The rendered workflow complements, rather than replaces, the automated denial/replay/concurrency/isolation evidence.

## Browser-discovered correlation defect

The first rendered quality acceptance exposed a defect invisible in status-only assertions: the attempt ID and the review's attempt reference were independently protected, so randomized protection produced different public strings and the UI could not correlate the immutable review to its attempt.

The response mapper now protects each acquisition-attempt identity once and reuses that exact public identity for every review reference in the same response. Integration coverage asserts the relationship explicitly. The rebuilt API and refreshed reviewer session then rendered `Quality: Accepted` under attempt 1.

## Verification

```text
Development fixture create:                  passed
Clinician Order/Queue handoff:                passed
Eligible operator Study/acquisition:          passed
Independent reviewer quality decision:       passed
Wrong-modality equipment excluded in UI:      passed
Role-separated command visibility:            passed
Queue/Study independence in browser:          passed
Public-ID review correlation after rebuild:   passed
Browser runtime errors:                       none in three sessions
Fixture transactional removal:               passed
Fixture idempotent second removal:             passed
Full multi-target Release solution build:     passed, 0 warnings / 0 errors
Unit tests:                                  100 passed
Architecture tests:                           9 passed
Integration tests:                           35 passed
Total automated tests:                       144 passed, 0 failed
Local SQL migration chain:                    current
EF model drift:                               none
Temporary fixture data/users/roles:           removed
Temporary API/Portal/browser sessions:        stopped/closed
```

## Deliberately deferred

- named Delhi clinical director, radiology lead and compliance validation of RAD-01 through RAD-09;
- approved protocol, deviation, abort and quality-reason catalogs plus effective policy/version administration;
- equipment registration/licence/QA/compliance evidence and tenant enablement gate;
- Study-status SignalR invalidation, reconnect and multi-node reconciliation;
- repeat-required rendered browser path, broad accessibility and automated browser execution in CI;
- Admin/MAUI execution experiences;
- RIS/PACS/DICOM worklist/storage/reconciliation integration;
- every interpreted Result/report/sign/amend/critical-communication/release function governed by RAD-10 through RAD-24.

## Achievement effect

DOC-033 advances conservatively:

- row 7, Portal: **85% → 86.25%**;
- row 9, shared UI: **85% → 86.25%**;
- row 16, dashboards/Queue/realtime: **89.5% → 89.75%**;
- row 20, quality evidence: **80% → 81.25%**.

The score gains 4 points: `1508.5 / 20 = 75.425%`, reported as **75.43%**.

Row 16 deliberately remains below 90%. A successful synthetic browser run does not replace named clinical/radiology/compliance acceptance, approved code/equipment controls, reconnect/multi-node evidence or tenant live-use authorization.

## Recommended next goal

Add privacy-safe Radiology Study status invalidation and authoritative reconciliation to the existing realtime boundary. Register/start/acquire/review remain durable HTTP commands; SignalR carries only branch/service-point scoped invalidation. Prove two-session automatic refresh, reconnect fallback, stale-version recovery and no Patient/clinical narrative in Hub payloads. Continue the RAD-01 through RAD-09 owner-validation track in parallel, and do not begin interpretation, reporting or release.
