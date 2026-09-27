# 17 — Nonfunctional, India Compliance and Security Preparation

Status: planning input, not legal or regulatory advice. Applicability and implementation dates require qualified Indian counsel and the clinic's operating state(s).

## India reference baseline

- The [Digital Personal Data Protection Act, 2023 and Digital Personal Data Protection Rules, 2025](https://www.meity.gov.in/documents/act-and-policies/digital-personal-data-protection-rules-2025-gDOxUjMtQWa) drive notice, lawful processing, consent where applicable, rights handling, reasonable security safeguards, breach processes, processors and retention preparation. Effective dates must be tracked by counsel.
- The [ABDM Health Data Management Policy](https://abdm.gov.in/static/media/health_management_policy_bac9429a79.80f74bc3e039c00acd4f.pdf) is a health-data privacy and consent design reference. ABDM/ABHA participation and integration scope must be explicitly approved; care cannot be denied solely for refusal to use an ABHA identifier.
- [EHR Standards for India 2016](https://www.mohfw.gov.in/sites/default/files/EMR-EHR_Standards_for_India_as_notified_by_MOHFW_2016_0.pdf) guide interoperable clinical structure, terminology, exchange, security and record handling.
- [AERB diagnostic-radiology requirements](https://www.aerb.gov.in/english/i-am-a-radiographer/diagnostic-radiology) are a blocking input for any X-ray/CT facility, equipment, qualified-operator, radiation-safety and quality-assurance workflow. BOOKDOC may track approved evidence and expiry; it cannot certify compliance or replace e-LORA/licensing controls.
- [NABH Medical Imaging Services standards](https://nabh.co/programmes/medical-imaging-services-accreditation-programme/) are a quality-system design reference for imaging identification, performance, interpretation, reporting, archiving, delivery and communication. Use does not claim accreditation.
- [CBIC GST invoice rules](https://cbic-gst.gov.in/gst-invoice-rules.html) are a billing design input where a tenant/supply is GST-applicable. Tenant tax advisers must approve classification, invoice fields, numbering, place of supply, exemptions and retention.
- Clinical Establishments legislation, state rules, professional-council rules, prescription requirements, biomedical/lab/radiology obligations and retention requirements vary by establishment and location; the first operating state is a blocking discovery input.

## Delhi pilot applicability preparation

- The [central Clinical Establishments portal](https://www.clinicalestablishments.mohfw.gov.in/en/about-us) states that the Clinical Establishments Act, 2010 is not currently applicable to the NCT of Delhi. Do not configure Delhi compliance by copying another state's CEA workflow.
- Delhi Health & Family Welfare publishes the [Delhi Nursing Homes Registration Act, associated forms and rules](https://health.delhi.gov.in/health/forms-act-rule). Counsel/clinic advisers must determine whether each outpatient Physiotherapy or Orthopaedic establishment and its services fall within applicable Delhi registration or another licence category.
- The [Delhi Council for Physiotherapy and Occupational Therapy](https://health.delhi.gov.in/health/delhi-physiotherapy-and-occupational-therapy-council) regulates and maintains the Delhi register for physiotherapists. Practitioner onboarding must be able to store council, registration number, validity/status evidence and verification history without hard-coding a single profession.
- Orthopaedic doctors require recognized medical qualification and valid registration/licence evidence under the applicable National/State Medical Council framework. The [NMC rules and regulations register](https://www.nmc.org.in/rules-regulations-nmc/) is a reference; the operative professional-conduct instrument and any Delhi Medical Council obligations must be confirmed at release review.
- Delhi-specific establishment, professional, fire/building, biomedical-waste, diagnostic/radiology, pharmacy and prescription obligations depend on actual services and premises. They remain an onboarding checklist owned by the clinic, with configurable evidence/expiry reminders rather than unsupported automatic certification. The X-ray/CT clinical and operational approval record is controlled by [DOC-057](57-xray-ct-execution-result-policy-decision-pack.md).

## Data responsibility assumptions to validate

The clinic customer will usually determine purposes of patient-care processing, while the SaaS provider processes tenant data under contract. BOOKDOC2026 may separately determine purposes for platform account, fraud, billing or support data. Counsel must map Data Fiduciary/Data Processor roles, notices, contracts, subprocessors, cross-border processing, significant-fiduciary obligations if designated, grievance handling and breach reporting.

## Security and privacy requirements

- tenant isolation at database, application, storage, cache, search, job, report, telemetry and backup layers;
- encryption in transit and at rest, managed secret storage and key rotation;
- MFA/risk-based protection for privileged users; short sessions/tokens and revocation;
- least privilege, field masking, break-glass controls and periodic access review;
- immutable audit for sensitive read/export, clinical signing, money, permissions and support elevation;
- data inventory, purpose, notice/consent or other lawful basis, retention and deletion/legal-hold rules;
- verified data-principal request and grievance workflows without deleting records that must lawfully be retained;
- incident response, breach assessment/notification decision log and tenant communication process;
- processor/subprocessor inventory, due diligence, contract controls and exit/deletion evidence;
- secure development, dependency scanning, threat modeling, penetration testing and log redaction.

## Service qualities to quantify

| Quality | Pre-development decision |
|---|---|
| Availability | target per host/module; planned maintenance and degraded modes |
| Recovery | RPO/RTO by clinical, billing, document and configuration data |
| Performance | p95 API/UI/report targets and expected tenant/branch concurrency |
| Scalability | tenant, patient, appointment, message, document and report volumes |
| Accessibility | WCAG target, keyboard/screen-reader/contrast and Indian language plan |
| Compatibility | supported browsers, Android/iOS versions, printers and label media |
| Observability | tenant-safe metrics/traces/logs, alerts, runbooks and service-level indicators |
| Retention | approved schedule per record/artifact/log/backup class and legal hold |

## Confirmed initial capacity envelope

| Dimension | Planning target |
|---|---:|
| Independent clinic tenants in year one | 10 |
| Independent clinic tenants by year three | 50 |
| Branches per tenant | normally fewer than 10 |
| Staff users in a typical clinic | 1–20 |
| Registered patients in a normal clinic tenant | up to 100,000 |
| Appointments per day in a typical clinic | approximately 100 |
| Appointments per day in the owner-operated large tenant | approximately 2,000 across branches |
| Large-tenant data volume | approximately 20× normal tenant |

These figures define minimum performance, migration, reporting, Worker-fairness and recovery test datasets. Concurrent-session peaks, document sizes, message volume and retention growth remain to be measured before production capacity approval.

## Mandatory pre-pilot evidence

Legal/state applicability register, data-flow map, privacy notice and consent inventory, processor agreements, threat model, tenant-isolation test, access review, backup/restore test, incident exercise, retention/deletion test, accessibility review and production-support approval.

Implementation ownership for evidence separation, privacy-safe observability, configuration/secrets, file lifecycle, localization/accessibility, supply-chain integrity and capability-specific recovery is defined in [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md). That technical plan does not replace the qualified legal and clinic approvals required by this document.
