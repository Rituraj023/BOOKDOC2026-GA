# BOOKDOC2026 Pre-development Artifact Package

Prepared: 2026-08-09  
Status: ready for stakeholder review; open and blocked items remain visible

Architecture notice (2026-08-17): API, Admin and Portal are the three product applications. AppHost is development-only orchestration, and Worker is an API-hosted class library rather than a separate production process. If a binary workbook, DOCX or PDF states otherwise, [Target Architecture](../../docs/03-target-architecture-and-new-logic.md) and [Backend Workers, Reporting and Printing](../../docs/11-backend-workers-reporting-printing.md) supersede that text until the binary approval pack is regenerated.

These working artifacts supplement the authoritative [Implementation Master](../../README.md). They support workshops, review and approval; they do not authorize implementation, production data movement, vendor purchase or go-live.

## Artifact navigator

| Artifact | Format | Purpose |
|---|---|---|
| [Requirements and Traceability](BOOKDOC2026-Requirements-and-Traceability.xlsx) | XLSX | Roles, permissions, workflows, requirements, acceptance/test traceability, risks and decisions |
| [Migration, Data and Reports](BOOKDOC2026-Migration-Data-and-Reports.xlsx) | XLSX | Legacy screen disposition, target data dictionary, migration mapping, report inventory and onboarding documents |
| [API and Operational Readiness](BOOKDOC2026-API-and-Operational-Readiness.xlsx) | XLSX | Proposed API/permission catalog, integrations, environments, implementation gates and volume assumptions |
| [Pre-development Approval Pack](BOOKDOC2026-PreDevelopment-Approval-Pack.docx) | DOCX | Editable stakeholder review, decision and sign-off document |
| [Pre-development Approval Pack — fixed PDF](../../output/pdf/BOOKDOC2026-PreDevelopment-Approval-Pack.pdf) | PDF | Fixed-layout review copy of the approval pack |
| [Architecture and Workflow Diagrams](../../docs/25-predevelopment-diagrams.md) | Markdown/Mermaid | Tenancy, onboarding, Worker and readiness-gate views |

## How to use the package

1. Review the approval pack and assign the named business, clinical, privacy, finance, architecture and operations owners.
2. Use the yellow status/decision fields in the workbooks during discovery workshops.
3. Attach evidence references, not credentials, patient data, raw database extracts or unredacted logs.
4. Update the matching Markdown plan whenever an approved workbook decision changes scope, architecture, estimates or a release gate.
5. Record formal approval only after all required evidence is independently reviewable.

## Current limitations

- Entries are a preparation baseline derived from source-code discovery and confirmed product decisions; the live production database has not been profiled.
- Report inventory starts from the observed 45 primary RDLC layouts and 43 export variants. Active use and ownership still require user validation.
- Employee and payroll design remains deferred until its source database and ownership rules are supplied.
- Provider selection, Delhi-specific clinical/legal review, recovery objectives and several volume assumptions remain approval inputs.
