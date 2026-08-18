# 29 — Stakeholder Party Reference Slice

Owner: Stakeholder + Patient Registry + backend  
Status: Verified reference slice  
Last reviewed: 2026-08-09

## Outcome

Shared party data is stored once. `stakeholder.stakeholder` represents either a Person or Corporate; subtype tables hold type-specific facts. Contacts, addresses, identifiers and document references belong to Stakeholder. `patient.patient` is a role and must reference a Person stakeholder.

This prevents future employee, practitioner, guardian, payer, employer, supplier and corporate records from each copying the same party data. A role module owns only its role-specific lifecycle and facts.

## Implemented ownership

| Table | Ownership |
|---|---|
| `stakeholder.stakeholder` | tenant party ID, type, display name, status, version |
| `stakeholder.person` | name, birth information, administrative sex |
| `stakeholder.corporate` | legal/trade names and registration number |
| `stakeholder.contact_point` | normalized Mobile/Email values, primary/verified state |
| `stakeholder.identifier` | typed, issuer-aware identifiers |
| `stakeholder.address` | multiple dynamically typed Indian addresses and primary marker |
| `stakeholder.document_reference` | dynamic `document_type_id` plus secure `file_id` and metadata; no file bytes |
| `patient.patient` | stakeholder link, patient number/status, registration idempotency, blood group |

Every relationship and uniqueness rule is tenant-scoped. A stakeholder cannot change tenant. A Patient cannot point to a Corporate stakeholder through the application factory.

## API and permissions

| Route | Permission |
|---|---|
| `POST /api/v1/branches/{branchId}/stakeholders/persons` | `Stakeholders.Manage` |
| `POST /api/v1/branches/{branchId}/stakeholders/corporates` | `Stakeholders.Manage` |
| `GET /api/v1/branches/{branchId}/stakeholders/{id}` | `Stakeholders.View` |
| requests containing document references | additionally `Stakeholders.Documents.Manage` |

Patient registration creates its Person stakeholder atomically and returns the stakeholder ID and both concurrency versions.

## Migration evidence

The earlier development-only party-refactor migration and GUID baseline were retired after the local database was reconfirmed empty. The current `NumericKeyBaseline` directly creates the final Stakeholder/Patient ownership model with non-identity `bigint` keys. This reset is valid only for the empty pre-production database; populated legacy migration still requires explicit source-to-Stakeholder mapping, crosswalks, reconciliation and reviewed rollback/forward-fix evidence.

## Verification and remaining gates

Automated coverage verifies person/patient invariants, corporate shared data, document permission, Patient API behavior, tenant scoping and real SQL Patient operations. Before production: connect `file_id` to the secure Documents module, add contact/address effective history and verification, approve identifier types and sensitive-field protection, and define party merge/unmerge workflows.
