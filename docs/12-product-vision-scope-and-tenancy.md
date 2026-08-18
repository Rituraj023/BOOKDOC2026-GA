# 12 — Product Vision, Scope and Tenancy

Status: pre-development baseline  
Market: India; first pilot jurisdiction Delhi  
Deployment model: multi-tenant SaaS for independent clinic customers

## Product vision

BOOKDOC2026 will provide one secure clinic-management platform for independent clinic businesses, their branches, staff and patients. It begins with outpatient clinic operations and preserves clean extension boundaries for future hospital modules.

Success means a clinic can onboard, configure roles, register patients, reserve providers and categorized resources, operate queues, document care, bill, communicate and report without sharing data or configuration with another clinic customer.

## Tenant model

- `Tenant` represents one independent clinic customer and is the primary security, data, configuration, subscription and operational boundary.
- A tenant contains one or more organizations/legal entities and branches.
- Human-readable patient, invoice and receipt numbering is tenant or branch scoped according to approved policy; internal identifiers remain globally unique.
- Users may hold different roles and branch scopes within one tenant. Cross-tenant membership is exceptional and must use separately audited grants.
- Patients may have accounts in more than one tenant, but tenant clinical records are not silently combined or shared.
- Platform operators use a separate control plane. Support access to tenant data is time-bound, reasoned, approved where required and fully audited.
- Every database query, job, report, document, cache key, SignalR group, file-storage key and print job carries durable tenant scope.

The confirmed commercial shape is one centrally operated BOOKDOC2026 deployment used as the owner's SaaS business. Year one targets 10 independent clinic tenants and year three targets 50. Each tenant will usually begin with one clinic and may create fewer than 10 branches. A typical clinic has 1–20 staff users, up to 100,000 registered patients and about 100 appointments per day. The owner's multi-clinic tenant may hold roughly 20 times the normal data volume and process about 2,000 appointments per day across its branches. These are initial sizing and test targets, not product/database limits.

Clinic onboarding has two entry paths: a clinic may fill and submit an application form, or a platform operator may directly register the application. Neither path activates a tenant automatically. A platform operator verifies the clinic and first owner using configurable document types linked to secured files, then explicitly approves or rejects activation through the control plane. Only platform operators may create, version, activate or retire document types. Required documents may vary by jurisdiction and clinic type and support issue/expiry dates, verification status and replacement lineage. Clinic applicants cannot approve themselves or another tenant.

Each tenant has default logo/branding, document numbering and communication identities. A tenant owner delegates an explicit branch-configuration permission to selected branch administrators. Those administrators can manage allow-listed branch overrides—for example branch logo/contact and invoice/receipt series—without BOOKDOC platform approval. Domains and email, WhatsApp or SMS identities remain unusable until any required provider/legal verification succeeds. Inheritance, effective version and verification state remain explicit, and historical issued documents retain the effective configuration snapshot.

## Clinic release boundary

Release scope includes platform onboarding, identity and permissions, clinic/branch setup, patient registry, workforce, generalized resource scheduling, OPD and departmental queues, encounters, prescriptions, basic investigation workflows, clinic billing, communications, reports, printing and staged MAUI experiences. Physiotherapy and Orthopaedics are the first specialty configurations; see [Physiotherapy and Orthopaedics Preparation](24-physiotherapy-orthopaedics-preparation.md).

Hospital admission, ward occupancy, nursing/MAR, pharmacy inventory, analyzer/PACS integration, operation theatre, emergency, insurance claims and payroll remain future modules. Bed-category reservation may be planned for clinic/day-care use, but it must not be represented as full inpatient occupancy.

## Product principles

1. Server authorization, not menu visibility, protects every action and record.
2. Clinical and financial issued records are immutable or corrected through explicit amendment/reversal.
3. Operational realtime updates always reconcile with authoritative API state.
4. Business background work is durable and executed by Worker-library services hosted inside API.
5. Every feature has tenant, role, branch, audit, privacy and failure behavior before implementation.
6. Legacy applications provide evidence; their architecture and unsafe behavior are not copied.
7. Employee/payroll remains deferred until its database and owners are supplied.

## Success measures to approve

- onboarding time for a new clinic/branch;
- patient registration and check-in time;
- booking/resource-conflict and duplicate-patient rates;
- queue wait time and abandoned-ticket rate;
- unsigned encounter and failed-message backlog;
- invoice/payment reconciliation variance;
- report completion time and failed scheduled jobs;
- tenant-isolation security test results;
- pilot adoption, support volume and recovery objectives.

## Decisions still required

Expected total tenants/users/transactions, SaaS onboarding method, subscription model, data-hosting provider/region, languages and branding model must be confirmed in [Pre-development Decisions and Questionnaire](23-pre-development-decisions-and-questionnaire.md).
