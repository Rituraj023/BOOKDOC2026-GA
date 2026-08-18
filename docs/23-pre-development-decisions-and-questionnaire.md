# 23 — Pre-development Decisions and Questionnaire

## Confirmed decisions

| ID | Decision | Status |
|---|---|---|
| PD-001 | Primary market is India | Confirmed 2026-08-09 |
| PD-002 | Initial roles include owner, administrator, receptionist, doctor, nurse, cashier, radiology technician, lab technician and patient | Confirmed 2026-08-09 |
| PD-003 | Product supports multiple independent clinic customers | Confirmed 2026-08-09 |
| PD-004 | Each independent clinic customer is a tenant; its branches sit below that tenant | Confirmed 2026-08-09 |
| PD-005 | Platform-operator control plane is separate from tenant Admin/Portal | Confirmed architecture interpretation 2026-08-09 |
| PD-006 | Delhi is the first pilot jurisdiction | Confirmed 2026-08-09 |
| PD-007 | Physiotherapy and Orthopaedics are the first specialties | Confirmed 2026-08-09 |
| PD-008 | One centrally operated SaaS deployment serves many clinic tenants; each tenant normally has fewer than 10 branches | Confirmed planning assumption 2026-08-09 |
| PD-009 | A typical clinic has 1–20 staff users | Confirmed 2026-08-09 |
| PD-010 | Platform operator approves every clinic through the control plane before tenant activation; initial onboarding is not immediate self-service | Confirmed 2026-08-09 |
| PD-011 | A normal clinic tenant may contain up to 100,000 registered patients; the owner's multi-clinic tenant may contain about 20 times the normal data volume | Confirmed planning target 2026-08-09 |
| PD-012 | Typical clinic appointment volume is about 100/day; the owner-operated large tenant may process about 2,000/day across branches | Confirmed planning target 2026-08-09 |
| PD-013 | Shared deployment initially targets 10 independent clinic tenants in year one | Confirmed planning target 2026-08-09 |
| PD-014 | Shared deployment targets approximately 50 independent clinic tenants by year three | Confirmed planning target 2026-08-09 |
| PD-015 | Clinic approval evidence uses configurable document-type IDs linked to secured files | Confirmed architecture direction 2026-08-09 |
| PD-016 | Tenants have their own branding, invoice numbering and communication identities; multiple branches may use controlled branch overrides | Confirmed 2026-08-09 |
| PD-017 | Only platform operators may create, version, activate or retire onboarding document types | Confirmed 2026-08-09 |
| PD-018 | Clinics may submit an onboarding form and platform operators may directly register a clinic; both require platform approval before activation | Confirmed 2026-08-09 |
| PD-019 | Tenant owners may request branch configuration overrides, but platform approval is required before activation | Superseded 2026-08-09 by PD-020 |
| PD-020 | Authorized branch administrators manage allow-listed branch overrides themselves without BOOKDOC platform approval; provider/legal verification may still gate domains and communication identities | Confirmed 2026-08-09 |

## Remaining discovery questions

Questions 3–13 and 18–20 can block relevant pilot modules. Expansion and commercial questions may be resolved later when they do not change the reference architecture.

### Clinic and launch

1. Beyond Delhi, which states should be prepared during the first commercial year?
2. What are the expected peak concurrent users, daily messages, documents/uploads and scheduled reports for a typical and large tenant?
3. Which initial dynamic document types and verification rules should Delhi Physiotherapy and Orthopaedic clinics require?
4. Which clinic-application fields may an operator correct during review, and which changes require resubmission by the applicant?
5. Which branch configuration items require external evidence or provider verification before use: domain, email sender, WhatsApp number or SMS identity? Invoice/receipt numbering remains self-managed but must satisfy uniqueness and issued-document immutability controls.

### Clinical and operational

6. Which roles may register walk-ins, overbook, prioritize queues, amend signed notes, issue prescriptions and verify/release lab or radiology results?
7. Is the first imaging scope scheduling/queue/status only, or does it include clinical reports and PACS/RIS exchange?
8. Which bed use is required in clinic release: day-care chair, observation bed, procedure bed or advance inpatient reservation?
9. Which current reports, receipt sizes, queue printers and label printers are actually active at pilot sites?

### Privacy, legal and data

10. Who will provide Indian legal/privacy and state clinical-establishment review?
11. Is ABDM/ABHA integration required for the pilot, a later clinic release or only an extension point?
12. What are approved retention periods and patient-request/grievance contacts?
13. Which cloud/provider and India region are preferred? Is any cross-border support or processing expected?

### Commercial and integrations

14. Is SaaS billing per tenant, branch, user, appointment or module? Are trials/suspension needed?
15. Which payment gateway, email provider, WhatsApp BSP and SMS/DLT setup already exist?
16. Which accounting/GST export or integration is required?
17. Which languages are required at pilot and public/patient launch?

### Migration and rollout

18. Which legacy database is authoritative for each pilot tenant, and can a read-only schema plus anonymized sample be supplied?
19. Is rollout big-bang, module-by-module or branch-by-branch, and how long must legacy read access remain?
20. Who are the named clinic product owner, clinical safety owner, finance owner, privacy contact, migration approver and go-live approver?

## Decision rule

Answers are recorded as dated decisions with owner and evidence. Unanswered items remain explicit dependencies; they are not filled with implementation assumptions. Any answer that changes product scope updates the [Implementation Master](../README.md), owning plan, effort range and document register.
