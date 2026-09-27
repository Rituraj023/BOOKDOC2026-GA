# 57 — X-ray/CT Execution and Result Policy Decision Pack

Status: Slice A provisionally approved for development and implemented; clinical-owner validation required before live use  
Owners: clinic clinical director + radiology lead + product + privacy/security + operations  
Pilot: Delhi clinic deployment; X-ray and CT only  
Last reviewed: 2026-08-28

## Purpose and authority boundary

This pack converts the open decisions from [DOC-056](56-radiology-technician-ordered-worklist.md) into an approval-ready execution and result model. On 2026-08-29 the product owner accepted the recommended RAD-01 through RAD-09 defaults as provisional development decisions, with explicit permission to revise them later. [DOC-058](58-radiology-study-acquisition-quality-foundation.md) implements that bounded backend slice. This document remains **not** a radiology practice protocol, does not decide what constitutes a critical finding, and does not authorize production clinical use. A Delhi clinical director, radiology lead and compliance owner must validate the Slice-A rules before tenant enablement.

The current backend implementation now reaches bounded Study registration/start, immutable acquisition attempts and explicit technical-quality review, as recorded in DOC-058. This document deliberately keeps the following independent:

1. operational Queue progress;
2. ordered clinical intent;
3. study/acquisition evidence;
4. technical quality acceptance;
5. interpreted report versions;
6. urgent/critical communication and acknowledgement;
7. clinical-recipient and patient release;
8. immutable distributed document snapshots.

Queue completion must never imply that images were acquired, quality accepted, interpreted, signed, communicated or released.

## Authoritative preparation evidence

The following sources constrain the design but do not replace a Delhi legal/compliance review or a clinic-approved clinical policy:

- [AERB Diagnostic Radiology](https://www.aerb.gov.in/english/i-am-a-radiographer/diagnostic-radiology) identifies licensing, qualified personnel, radiation-safety and modality-specific quality-assurance material for diagnostic X-ray facilities.
- [AERB Safety Code for diagnostic X-ray equipment](https://www.aerb.gov.in/storage/uploads/documents/regdocMS24h.pdf) governs radiation safety in the design, installation and operation of medical diagnostic X-ray equipment. BOOKDOC records evidence and references; it does not certify regulatory compliance.
- [NABH Medical Imaging Services programme](https://nabh.co/programmes/medical-imaging-services-accreditation-programme/) treats requisition, identification, performance, interpretation, reporting, image archiving, report delivery and quality control as controlled imaging processes. NABH accreditation is not claimed by this plan.
- [NABH Medical Imaging Services standards preview](https://testing.nabh.co/wp-content/uploads/2025/07/5.-Standards-for-MIS-2nd-Edition_Edited.pdf) calls for standardized reports, recall/amendment guidance, and defined routine/urgent/critical communication turnaround.
- [MoHFW EHR Standards for India 2016](https://www.mohfw.gov.in/sites/default/files/EMR-EHR_Standards_for_India_as_notified_by_MOHFW_2016_0.pdf) supports least-privilege access, recorded viewing/creation/printing, user/time/record audit evidence, integrity protection and traceable correction.
- [ABDM Health Data Management Policy](https://abdm.gov.in/static/media/health_management_policy_bac9429a79.80f74bc3e039c00acd4f.pdf) supplies privacy-by-design, federated storage and consent-based sharing direction when participating in the ABDM ecosystem.
- [Digital Personal Data Protection Rules, 2025](https://www.meity.gov.in/static/uploads/2025/11/53450e6e5dc0bfa85ebd78686cadad39.pdf) include staged commencement. Launch review must confirm which provisions are then in force; the design nevertheless prepares for access control, masking/tokenization, monitoring, processing logs, breach handling and retention controls.

## Product boundary

Included in the first policy:

- one X-ray or CT service per existing Investigation Order;
- one study root with one or more immutable acquisition attempts;
- technician/operator identity, equipment/resource reference and timing;
- protocol/version reference and reasoned deviation;
- technical-quality decision and repeat/abandon evidence;
- versioned preliminary/final/amended reports where enabled;
- critical-result communication attempts and explicit acknowledgement;
- separately authorized clinical and patient release;
- immutable PDF/document snapshots for every officially distributed signed report.

Excluded until separately designed:

- raw DICOM/image storage in the BOOKDOC database;
- PACS/RIS modality worklist implementation or DICOM conformance claims;
- radiation-dose thresholds, diagnostic decision support or automatic interpretation;
- ultrasound/PC-PNDT, MRI, mammography, nuclear medicine and interventional workflows;
- contrast, sedation, pregnancy-screening or procedure-specific clinical protocols;
- AI inference, triage or autonomous critical-result classification;
- laboratory results and multi-item panels;
- billing decisions derived from acquisition or report state.

External images belong in an approved PACS/VNA. BOOKDOC may store protected external identifiers, endpoint-independent study references, checksums and reconciliation status, never credentials or a public PACS URL.

## Recommended state model

### Order

The existing Order remains the clinical request. Proposed later statuses are `Requested`, `Accepted`, `Cancelled` and `Completed`. `Completed` means all required study/report obligations for the tenant policy are satisfied; it is never copied from Queue state.

### Study execution

One `RadiologyStudy` is created for an accepted Order. Its proposed state is derived from immutable commands/events:

```text
Registered -> InProgress -> Acquired -> QualityAccepted
                    |            |
                    |            +-> RepeatRequired -> InProgress
                    +----------------> Aborted
Registered --------------------------> Cancelled
```

Rules:

- start requires an authorized, currently eligible operator and active matching-modality equipment/resource;
- every acquisition attempt has its own sequence, performer, start/end time and outcome;
- `Acquired` records completion of one attempt, not diagnostic adequacy;
- a quality reviewer explicitly accepts or requests a repeat;
- repeat does not overwrite the rejected attempt and requires a coded reason plus optional bounded note;
- abort/cancel requires a reason and does not fabricate a completed Queue or Result state;
- equipment licence/QA evidence may gate production operation after its source and validation rules are approved.

### Result/report

One `RadiologyReport` belongs to one quality-accepted study. Every save creates a version; signed versions are immutable.

```text
Draft -> PreliminarySigned (optional) -> FinalSigned -> AmendedSigned
```

Rules:

- preliminary reporting is disabled by default for the clinic pilot until explicitly approved;
- only an eligible interpreter may author/sign according to the tenant policy;
- final signing requires the latest quality-accepted acquisition and an eligible signer;
- a final report cannot be edited, withdrawn or deleted;
- correction creates a reasoned amendment that references the superseded signed version;
- the prior version and all distributed snapshots remain available to specifically authorized users;
- no report content is written to general audit, SignalR payloads or Worker diagnostic logs.

Recommended report sections are procedure/technique, comparison, findings, impression, recommendation and criticality. The clinical owner approves required fields, vocabulary and modality-specific templates before implementation.

### Critical-result communication

Criticality is a clinical assertion made by an eligible interpreter using a tenant-approved classification and policy version. The software must not infer it from words, codes, Queue priority or turnaround time.

Proposed communication state:

```text
Required -> Attempted -> Acknowledged
             |
             +-> EscalationDue -> Attempted
```

Every attempt records the report version, intended recipient role/person, approved channel, actor or Worker delivery reference, time and outcome. Acknowledgement records the accountable clinical recipient and time. Delivery, opening a report and acknowledgement are distinct facts. Failed delivery remains visible and follows the approved escalation ladder; SignalR may invalidate a dashboard but cannot perform or acknowledge the durable command.

### Release and snapshots

Clinical-recipient release and patient release are separate commands.

- Only a `FinalSigned` or `AmendedSigned` version can be officially released.
- Preliminary versions are never patient-released under the proposed pilot default.
- Clinical release goes only to permitted care-team/referring recipients in durable scope.
- Patient release is manual by default until the clinic approves automatic timing, exceptions, identity verification and representative/minor access.
- A critical report is never silently auto-released to the patient under the proposed default; the approved policy decides whether acknowledgement or clinician review is a prerequisite.
- Each official release points to an immutable generated snapshot with content hash, template/version, signer evidence, recipient class and release time.
- An amendment produces a new snapshot and marks earlier distributions superseded; it never rewrites or deletes them.
- Email, WhatsApp and push carry a notification or secure-link instruction, not report narrative or an unrestricted attachment, unless a separately approved channel/privacy policy permits it.

## Responsibility model

Role names do not grant authority by themselves. Commands require host permission, durable tenant/branch scope and effective professional/resource eligibility.

| Capability | Proposed responsible actor | Default constraint |
|---|---|---|
| See ordered work | permitted radiology technician/operator | bounded worklist only |
| Start/record acquisition | eligible X-ray/CT operator | matching branch, modality and active equipment |
| Record protocol/deviation | performing operator | approved protocol version; reason mandatory for deviation |
| Accept quality/request repeat | senior technologist or radiologist chosen by clinic | separately permissioned; self-review policy explicit |
| Create report draft | eligible interpreter | quality-accepted study only |
| Sign preliminary | eligible interpreter if feature enabled | disabled by default |
| Sign final/amendment | clinic-approved eligible interpreter | verified credential and effective assignment |
| Classify criticality | eligible final/preliminary signer | tenant-approved policy/version only |
| Communicate critical result | assigned clinical/radiology team | durable attempts and escalation |
| Acknowledge critical result | accountable clinician/approved recipient | recipient identity and time required |
| Release clinically | authorized radiology/records role | signed version and recipient scope |
| Release to patient | authorized records/clinical role | manual default; identity/consent policy |
| Manage templates/policy | authorized Admin configuration role | no standing right to read report narrative |
| Operate queues/jobs | API/Worker | no clinical interpretation authority |

Platform operators and Admin technical support receive no standing clinical access. Break-glass access needs a future separately approved, time-limited, reasoned and alerted control.

## Proposed permission catalog

DOC-058 implements the following Slice-A permission names:

```text
Radiology.Studies.View
Radiology.Studies.Start
Radiology.Acquisitions.Record
Radiology.Studies.QualityReview
```

The following remain reserved proposals and are not implemented permissions:

```text
Investigations.Reports.View
Investigations.Reports.Draft
Investigations.Reports.PreliminarySign
Investigations.Reports.FinalSign
Investigations.Reports.Amend
Investigations.CriticalResults.Communicate
Investigations.CriticalResults.Acknowledge
Investigations.Reports.ReleaseClinical
Investigations.Reports.ReleasePatient
Investigations.ReportSnapshots.View
Investigations.Policy.Manage
```

`Investigations.Worklist.View`, generic Queue permissions, the implemented Radiology permissions and every proposed permission above remain independent. Permission composition is tested without assuming a named role.

## Proposed persistence design

| Table/concept | Minimum responsibility |
|---|---|
| `radiology.study` | tenant/branch/order/patient/modality root, current derived status and optimistic version |
| `radiology.study_event` | append-only transition, actor, reason, version and time |
| `radiology.acquisition_attempt` | immutable attempt sequence, operator, equipment, protocol version, start/end, outcome and protected PACS reference |
| `radiology.quality_review` | immutable reviewer decision, acquisition attempt, reason and time |
| `radiology.report` | study root and current latest-version pointer/status |
| `radiology.report_version` | immutable version content, author, clinical policy/template version, criticality and content hash |
| `radiology.report_signature` | signed version, eligible signer, signature method/evidence and time |
| `radiology.critical_communication` | report version, recipient, channel, attempt/escalation outcome and Worker correlation |
| `radiology.critical_acknowledgement` | communication/report version, accountable recipient and acknowledged time |
| `radiology.report_release` | version, audience/recipient scope, actor, policy basis, snapshot and time |
| `document.artifact` | immutable generated PDF metadata/content reference under DocumentService ownership |

Every clinical table uses positive signed `long` keys, tenant query filters and tenant-bearing foreign keys. Public contracts use type/tenant-bound protected strings. Clinical narrative is never duplicated into Order/Queue/audit/outbox rows. Actor identity is retained even after account deactivation.

The report version stores the exact signed content or a canonical immutable representation sufficient to reproduce and verify the distributed snapshot. DocumentService renders the snapshot; it does not own clinical truth or decide release.

## Proposed command/API boundary

Commands should be explicit and versioned rather than generic CRUD:

```text
POST .../investigation-orders/{orderId}/study/start
POST .../studies/{studyId}/acquisitions
POST .../studies/{studyId}/quality-reviews
POST .../studies/{studyId}/reports/versions
POST .../reports/{reportId}/preliminary-signatures
POST .../reports/{reportId}/final-signatures
POST .../reports/{reportId}/amendments
POST .../report-versions/{versionId}/critical-communications
POST .../critical-communications/{communicationId}/acknowledgements
POST .../report-versions/{versionId}/clinical-releases
POST .../report-versions/{versionId}/patient-releases
```

Each mutation carries an idempotency request ID and expected aggregate version where applicable. The API owns authorization, validation and durable transaction boundaries. Worker performs approved notification/snapshot jobs from durable outbox messages. SignalR publishes only tenant/branch-scoped invalidation/status metadata after commit.

## Decision register

`Recommended` is a safe product default, not an approval. Each tenant may choose only an allow-listed policy variant; clinical safety rules cannot be weakened through arbitrary branch configuration.

| ID | Decision required | Recommended default | Required approver | Status |
|---|---|---|---|---|
| RAD-01 | eligible X-ray and CT operators | verified qualification plus effective branch/modality assignment | clinical director + radiology lead | Provisional product approval; implemented; clinical validation pending |
| RAD-02 | equipment/licence/QA gate | active matching resource with current approved compliance evidence | radiology lead + compliance | Partially implemented: active/matching resource; compliance-evidence source pending validation/implementation |
| RAD-03 | protocol catalog and version owner | radiology lead owns centrally approved tenant protocols | radiology lead | Provisional input capture implemented; approved catalog/owner validation pending |
| RAD-04 | mandatory protocol/deviation fields | protocol/version always; coded reason and bounded note for deviation | radiology lead | Provisional product approval; implemented; code-set validation pending |
| RAD-05 | who accepts technical quality | separate senior-technologist or radiologist permission | clinical director + radiology lead | Provisional separate permission/eligibility implemented; actor mapping validation pending |
| RAD-06 | whether self-quality-review is allowed | disabled unless small-clinic policy explicitly allows and flags it | clinical director | Provisional no-self-review default implemented; clinical validation pending |
| RAD-07 | repeat-exposure approval and reason set | explicit quality-review decision; preserve every attempt | radiology lead | Provisional product approval; implemented; reason-set validation pending |
| RAD-08 | abort/cancel reasons and downstream action | coded reason; no automatic result/order/Queue completion | radiology lead + operations | Abort and state independence implemented; cancel workflow/code-set validation pending |
| RAD-09 | PACS/VNA identifiers and reconciliation | protected external study reference; no image blob in SQL | architecture + radiology IT | Protected reference presence implemented; PACS reconciliation pending |
| RAD-10 | eligible report authors/signers | verified interpreter credential plus effective assignment | clinical director | Pending |
| RAD-11 | preliminary reports | disabled for pilot | clinical director | Pending |
| RAD-12 | required report sections/templates | technique, comparison, findings, impression, recommendation, criticality | radiology lead | Pending |
| RAD-13 | final signing/countersigning | one eligible signer; countersigning only if approved by role/training policy | clinical director | Pending |
| RAD-14 | amendment and recall workflow | reasoned immutable amendment; prior version/snapshots retained and superseded | clinical director + records owner | Pending |
| RAD-15 | routine/urgent/critical definitions and turnaround | tenant policy/version; no software inference | clinical director + radiology lead | Pending |
| RAD-16 | critical-result recipient and escalation ladder | accountable clinician, timed escalation, named fallback roles | clinical director + operations | Pending |
| RAD-17 | what counts as acknowledgement | explicit authenticated recipient command; delivery/open is insufficient | clinical director | Pending |
| RAD-18 | clinical release timing | release signed final/amendment to permitted care team | clinical director | Pending |
| RAD-19 | patient release timing/exceptions | manual; final/amended only; critical cases require approved review rule | clinical director + privacy | Pending |
| RAD-20 | minor/representative access | use verified Patient relationship/authority and consent policy | privacy/legal + clinical director | Pending |
| RAD-21 | snapshot format, signature evidence and retention | immutable PDF/hash plus version/signer/release metadata; retention set by approved schedule | records + privacy/legal | Pending |
| RAD-22 | notification channels/content | secure notification/link; no narrative by default | privacy + communications owner | Pending |
| RAD-23 | downtime and reconciliation | paper/PACS downtime procedure with later reasoned reconciliation; no backdated actor | operations + radiology lead | Pending |
| RAD-24 | target turnaround/SLA dashboards | measure policy-defined milestones without auto-changing clinical state | radiology lead + operations | Pending |

## Required negative and lifecycle tests

Before any production-capable slice, tests must prove:

- Queue completion cannot create/complete a study or report;
- wrong-tenant, wrong-branch and wrong-modality protected IDs fail closed;
- worklist permission alone cannot acquire, quality-review, report, sign, acknowledge or release;
- a deactivated/revoked/ineligible actor cannot start or sign even with a stale token;
- acquisition retry/idempotency cannot duplicate exposure records;
- stale aggregate versions cannot overwrite a concurrent quality/report decision;
- rejected attempts remain immutable and visible to authorized reviewers;
- an unaccepted-quality study cannot receive a final report;
- a draft or preliminary version cannot be patient-released;
- a final signed version cannot be edited or deleted;
- amendment links the superseded version and creates a new snapshot;
- communication delivery/open does not satisfy critical acknowledgement;
- escalation remains due after failed/expired attempts and survives Worker restart;
- report narrative never appears in SignalR, audit metadata, general logs or dead-letter errors;
- snapshot hash/content/version/signer match the released report;
- revoked patient/representative scope cannot fetch a snapshot;
- cross-branch Admin/support access is denied without explicit clinical scope;
- PACS/provider timeout is reconciled without inventing a study/result state;
- all read, print, export, sign, amend, communicate and release actions produce privacy-safe evidence.

## Ten owner-acceptance scenarios

1. Normal X-ray: ordered, acquired, quality accepted, final signed, clinically released and manually patient-released.
2. CT acquisition with approved protocol deviation and no report-content leakage to operational logs.
3. Quality rejection and repeat with both attempts retained and correctly attributed.
4. Procedure aborted after start with reason and no false Order/Queue/Result completion.
5. Preliminary reporting disabled actor attempts preliminary sign and is denied.
6. Critical final report triggers durable communication, failed first attempt, escalation and explicit clinician acknowledgement.
7. Final report amended after release; old snapshot remains verifiable and recipients receive the approved correction notification.
8. Signer credential/assignment revoked between draft and sign; signing is denied.
9. Patient representative attempts access without current authority; snapshot access is denied and audited safely.
10. PACS/Worker outage and recovery reconcile external reference, snapshot and notifications without duplicate clinical events.

## Implementation sequence and gates

### Slice A — study execution facts (backend reference implemented)

RAD-01 through RAD-09 received provisional product-development approval on 2026-08-29. DOC-058 implements `study`, immutable acquisition attempts, quality reviews, eligibility/permissions, explicit API commands, migration and tests. Tenant live use still requires clinical/radiology/compliance validation plus the named missing policy artifacts. Reporting and release are not added.

### Slice B — versioned interpretation

Requires RAD-10 through RAD-15 plus approved report templates. Implement report versions, signing/amendment and immutable content hashes. Do not enable patient release merely because final signing exists.

### Slice C — critical communication and release

Requires RAD-16 through RAD-24, privacy/channel decisions and DocumentService snapshot acceptance. Implement durable critical communication/acknowledgement, clinical release, patient release and immutable snapshots.

No slice may be enabled for a tenant without its effective policy/version and role-to-permission/eligibility mapping. Database migrations are additive and must not convert existing Queue completion into clinical state.

## Completion statement

The decision pack is complete as a control record. Slice A received provisional product-development approval; its backend reference is implemented in DOC-058 and its role-facing synthetic Portal journey is verified in [DOC-059](59-portal-radiology-execution-quality-workspace.md). The clinical live-use gate is not passed. RAD-10 through RAD-24 remain pending and RAD-01 through RAD-09 still require named clinical/radiology/compliance validation. Therefore:

- Study/acquisition/technical-quality backend code and migration are documented by DOC-058; no interpreted Result, report, communication or release is authorized;
- the evidence-based DOC-033 architecture-achievement score is **75.425%** (**75.43%** reported);
- Slice A cannot be enabled for a tenant until the named validators approve its effective policy, code sets, actor mapping and equipment compliance gate;
- authenticated operator-to-reviewer browser acceptance is complete as synthetic engineering evidence, while owner UAT, Study realtime/reconnect and tenant enablement remain open gates.

## Recommended next goal

Add the privacy-safe Study-status invalidation/reconciliation follow-up from DOC-059 while conducting the Delhi validation workshop for RAD-01 through RAD-09. Record approver names, effective policy/version, operator/reviewer mapping, equipment compliance source and approved reason/protocol catalogs before tenant enablement. Do not begin report signing, critical communication or patient release; RAD-10 through RAD-24 remain pending.
